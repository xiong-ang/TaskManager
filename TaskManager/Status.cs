using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Barret.Xiong
{
    public enum Status
    {
        //Pending to be registered
        Pendding,
        //Waitting for process
        Waitting,
        //Running
        Running,
        //Canceling
        Canceling,
        //Process is paused and waitting for user input
        Paused,
        //Task finished successfully
        Succeed,
        //Task failed during process
        Failed,
        //Task has been sucessfully canceled
        Canceled
    }
}
