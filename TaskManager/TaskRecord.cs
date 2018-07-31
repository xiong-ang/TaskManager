using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Barret.Xiong
{
    public class TaskRecord
    {
        public readonly IAsyncTask Task;
        public Status Status;
        public Task RunningTask;

        public readonly string Name;
        public readonly string DetailInfo;

        public DateTime RegisterationTime;

        public TaskRecord(IAsyncTask task, string name = "", string detail = "")
        {
            if (null == task)
                throw new NullReferenceException();

            Task = task;
            Status = Status.Pendding;
            RunningTask = null;
            Name = name;
            DetailInfo = detail;
        }
    }
}
