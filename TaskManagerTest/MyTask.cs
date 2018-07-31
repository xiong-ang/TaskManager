using Barret.Xiong;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace TaskManagerTest
{
    class MyTask:IAsyncTask
    {
        public string ProgressInfo { get; set; }

        public byte Progress { get; set; }

        public string ReferenceKey { get; set; }

        public MyTask()
        {
            ProgressInfo = string.Empty;
            Progress = 0;
            ReferenceKey = new Guid().ToString();
        }

        private string _userInput;
        public async Task ExecuteAsyncThread()
        {
            await Task.Run(async() => {
                for (int i = 0; i <= 100; i++)
                {
                    await Task.Delay(50);
                    Progress = (byte)i;
                    ProgressInfo = "Progress: " + Progress + "%";
                    Changed.Invoke(this, new EventArgs());
                    Console.WriteLine(ProgressInfo);

                    if(i == 50)
                    {
                        ResponseRequired.Invoke(this, new EventArgs());
                        while (string.IsNullOrWhiteSpace(_userInput))
                        {
                            await Task.Delay(500);
                        }
                    }
                }
                Console.WriteLine("Input: "+_userInput);
            });
        }

        public System.Threading.CancellationTokenSource TokenSource{get;set;}

        public event EventHandler ResponseRequired;

        public void UserEntry(string input)
        {
            _userInput = input;
        }

        public event EventHandler Changed;

        public void Dispose()
        {
            TokenSource.Dispose();
        }
    }
}
