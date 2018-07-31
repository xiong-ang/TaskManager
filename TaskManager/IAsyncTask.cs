using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Barret.Xiong
{
    public interface IAsyncTask:IDisposable
    {
        string ProgressInfo { get; set; }
        byte Progress { get; set; }
        string ReferenceKey { get; set; }
        Task ExecuteAsyncThread();

        //Control task cancellation, obtain from TaskManager
        CancellationTokenSource TokenSource { get; set; }

        //Event trigged when task require action from user
        event EventHandler ResponseRequired;

        //This method is called with user response for event responserequired
        void UserEntry(string input);

        //Event trigged when any information on task is changed
        event EventHandler Changed;
    }
}
