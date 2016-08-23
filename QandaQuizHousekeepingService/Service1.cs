using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace QandaQuizHousekeepingService
{
    public partial class Service1 : ServiceBase
    {
        private Timer quizElapsedCheckTimer = null;

        private bool alreadyStarted = false;                
        private int lastMinCalled = -1;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            quizElapsedCheckTimer = new Timer();
            this.quizElapsedCheckTimer.Enabled = true;
            this.quizElapsedCheckTimer.Interval = 2000;
            this.quizElapsedCheckTimer.Elapsed += new ElapsedEventHandler(timer_tick);
            this.quizElapsedCheckTimer.Start();
        }

        private void timer_tick(object sender, ElapsedEventArgs e)
        {
            int iMinute = -1;            

            if (!alreadyStarted)
            {                
                //check if its 15 min past of an hour 
                iMinute = DateTime.Now.Minute;
                
                if ((iMinute == 15 || iMinute == 30 || iMinute == 45 || iMinute == 00) && (iMinute != lastMinCalled))
                {
                    alreadyStarted = true;
                    
                    QandaQuizHousekeepingService.Housekeeping.runMundaneTasks();

                    lastMinCalled = DateTime.Now.Minute;
                    alreadyStarted = false;
                }
            }
        }

        protected override void OnStop()
        {
        }
    }
}
