using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Barret.Xiong
{
    public class TaskManager
    {
        private readonly ConcurrentDictionary<string, TaskRecord> TaskList = new ConcurrentDictionary<string, TaskRecord>();
        private static int TaskCounter; // All Tasks, Includes finished tasks
        private static readonly CancellationTokenSource EngineCts = new CancellationTokenSource();
        public static int MaxTaskCount { get; set; }

        #region Signalton and Start
        private static TaskManager _instance;
        private static readonly object SLock = new object();
        private static TaskManager Instance
        {
            get
            {
                lock (SLock)
                {
                    return _instance ?? (_instance = new TaskManager());
                }
            }
        }
        private TaskManager()
        {
            TaskManager.MaxTaskCount = 20;//Default

            if (EngineCts.IsCancellationRequested)
                return;
            Task.Factory.StartNew<Task>(Engine, EngineCts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        private static async Task Engine()
        {
            try
            {
                do
                {
                    await Task.Delay(200);
                    if (Instance.TaskList.Count(task => Status.Waitting == task.Value.Status) <= 0)
                        continue;

                    StartTasks();//Start a waiting task
                } while (!EngineCts.IsCancellationRequested);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static void StartTasks()
        {
            //Select only running tasks
            var runningTasks = Instance
                .TaskList
                .Where(t => t.Value.Status == Status.Running || t.Value.Status == Status.Paused || t.Value.Status == Status.Canceling)
                .ToList();

            //Ensure running tasks num <= MaxTasks
            var diff = MaxTaskCount - runningTasks.Count;
            if (diff <= 0)
                return;

            var taskToExecute = Instance
                .TaskList
                .FirstOrDefault(t => t.Value.Status == Status.Waitting);

            if (taskToExecute.Value == null) return;

            //Run task
            taskToExecute.Value.Task.TokenSource = new CancellationTokenSource();
            taskToExecute.Value.RunningTask = Task.Run(() => taskToExecute.Value.Task.ExecuteAsyncThread(), taskToExecute.Value.Task.TokenSource.Token);
            taskToExecute.Value.RunningTask.ContinueWith(task => UpdateTask(taskToExecute));

            ChangeStatus(taskToExecute.Key, Status.Running);
        }
        #endregion Signalton and Start

        #region Registe task
        public static string Register(IAsyncTask task, string name, string detailInfo = "", bool allowDuplicate = true)
        {
            if (EngineCts.IsCancellationRequested
                || string.IsNullOrWhiteSpace(task.ReferenceKey)
                || string.IsNullOrWhiteSpace(name)
                || !allowDuplicate && Instance.TaskList.Any(registredTask => registredTask.Value.Task.ReferenceKey.Equals(task.ReferenceKey)))
                return string.Empty;

            return Instance.RegisterImpl(task, name, detailInfo);
        }

        private static readonly object RLock = new object();
        private string RegisterImpl(IAsyncTask task, string taskName, string detailInfo = "")
        {
            string name;
            string key;
            lock (RLock)
            {
                TaskCounter++;
                name = TaskCounter + task.ReferenceKey;
                key = TaskCounter + taskName;
            }

            if (!TaskList.TryAdd(key, new TaskRecord(task, name, detailInfo))) return key;

            ChangeStatus(key, Status.Waitting);
            TaskList[key].RegisterationTime = DateTime.Now;
            task.ResponseRequired += TaskEvent;
            task.Changed += TaskChanged;

            return key;
        }
        #endregion Registe task

        #region Cancel/Clean/Dispose
        public static void CancelAllTasks()
        {
            Instance.TaskList
                .Where(task => task.Value.Status == Status.Pendding || task.Value.Status == Status.Waitting)
                .ToList()
                .AsParallel()
                .ForAll(t =>
                {
                    ChangeStatus(t.Key, Status.Canceled);
                    CleanTask(t);
                });

            Instance.TaskList
                .Where(task => task.Value.Status == Status.Running)
                .ToList()
                .AsParallel()
                .ForAll(t =>
                {
                    ChangeStatus(t.Key, Status.Canceling);
                    CancelTask(t);
                });
        }

        private static void CancelTask(KeyValuePair<string, TaskRecord> obj)
        {
            if (string.IsNullOrWhiteSpace(obj.Key) || null == obj.Value)
                return;

            obj.Value.Task.TokenSource.Cancel();
        }

        private static void CleanTask(KeyValuePair<string, TaskRecord> t)
        {
            if (t.Value.RunningTask != null)
            {
                t.Value.RunningTask.Dispose();
                t.Value.RunningTask = null;
            }
            t.Value.Task.Dispose();
            TaskRecord outer;
            Instance.TaskList.TryRemove(t.Key, out outer);
        }

        public void Dispose()
        {
            EngineCts.Cancel();//Stop Engine
            CancelAllTasks();

            //wait for all tasks cancelation, but only 2min
            for (int i = 0; i < 60; i++)
            {
                Thread.Sleep(1000);
                foreach (var task in TaskList)
                {
                    if (task.Value.RunningTask != null)
                        IsTaskFinished(task);
                }
            }
            TaskList.Clear();
        }

        public static void Release()
        {
            Instance.Dispose();
        }
        #endregion Cancel/Clean/Dispose

        #region Update and EventHandler
        private static void ChangeStatus(string key, Status status)
        {
            TaskRecord taskRecord;
            if (!Instance.TaskList.TryGetValue(key, out taskRecord))
                return;
            taskRecord.Status = status;
        }

        private static void UpdateTask(KeyValuePair<string, TaskRecord> task)
        {
            if (task.Value == null || task.Value.Task == null)
                return;
            IsTaskFinished(task);
        }

        private static void IsTaskFinished(KeyValuePair<string, TaskRecord> task)
        {
            if (task.Value.RunningTask == null)
                return;

            if (task.Value.RunningTask.IsFaulted)
            {
                ChangeStatus(task.Key, Status.Failed);
                CleanTask(task);
            }

            if (task.Value.RunningTask.IsCanceled)
            {
                ChangeStatus(task.Key, Status.Canceled);
                CleanTask(task);
            }

            if (task.Value.RunningTask.IsCompleted)
            {
                ChangeStatus(task.Key, Status.Succeed);
                CleanTask(task);
            }
        }

        private void TaskChanged(object sender, EventArgs e)
        {
            var task = TaskList.FirstOrDefault(t => t.Value.Task.Equals(sender));

            if (task.Key == null) return;
            UpdateTask(task);
        }

        private void TaskEvent(object sender, EventArgs e)
        {
            var task = TaskList.FirstOrDefault(t => t.Value.Task.Equals(sender));

            if (task.Key == null) return;
            ChangeStatus(task.Key, Status.Paused);
            UpdateTask(task);
        }
        #endregion Update and EventHandler

        #region user Input
        public static void Response(string key, string input)
        {
            var taskRecord = Instance.TaskList.FirstOrDefault(t => string.Equals(t.Key, key));
            if (taskRecord.Value == null || taskRecord.Value.Task == null)
                return;
            if (taskRecord.Value.Status != Status.Paused)
                return;
            ChangeStatus(taskRecord.Key, Status.Running);
            taskRecord.Value.Task.UserEntry(input);
        }
        #endregion user Input
    }
}