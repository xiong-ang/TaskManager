using Barret.Xiong;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TaskManagerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            TaskManager.MaxTaskCount = 20;
            var key = TaskManager.Register(new MyTask(), "MyTask");
            var input = Console.ReadLine();
            if (string.Equals(input, "stop"))
                TaskManager.CancelAllTasks();
            else
                TaskManager.Response(key, input);

            while(true)
            {
                Thread.Sleep(500);
            }
            TaskManager.Release();
        }
    }
}
