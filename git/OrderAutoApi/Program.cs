using Deduce.Common.Utility;
using Deduce.DMIP.ResourceManage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OrderAutoApi
{
    class Program
    {

        static void Main(string[] args)
        {
            //123
            AutoOrderModel order = new AutoOrderModel();
            order.Startup();
            while (true)
            {
                Thread.Sleep(1000);
            }
        }


    }

    public class AutoOrderModel
    {
        ETimer _timer = new ETimer();

        public AutoOrderModel()
        {
            _timer.Run += new RunAction(AutoOrderAct);
        }

        public void Startup()
        {
            _timer.Start(new TimeSpan(0, 1, 10));
        }

        private void AutoOrderAct()
        {
            string url = @"http://localhost:8801/api/Order/AutoDealOrder";
            var response = HttpUtil.Post(new HttpItems()
            {
                URL = url,
                Accept = "application/json;charset=utf-8;",
                ContentType = "application/json",
                Method = "POST",
                Timeout = 5 * 60 * 1000,
            }).Document;
            for (int i = 0; i < 30; i++)
            {
                Console.WriteLine(DateTime.Now.ToString()+"当前自动订单为：" + response);
                Thread.Sleep(4000);
            }
        }
    }
}
