using Microsoft.AspNetCore.Mvc;
using System.Net;
using UPMessageControlCore;

namespace SmartSoft_BackEnd_ConnectionTest.Controllers
{
    [Route("[controller]")]
    public class GetSetSmartSoft : ControllerBase
    {
        IMessageControl messageControl;
        private bool ConnectFlag = false;
        private int errorcode = 0;
        private int senderrorcode = 0;
        private int Reverrorcode = 0;
        private string errormsg = "";
        private string SendUID = "";
        private string revmessage = "";

        //通信の状態が変わった場合に発生するイベント
        void ConnectionChanged(bool Connected, int ECode, string EMessage)
        {
            ConnectFlag = Connected;
            errorcode = ECode;
            errormsg = EMessage;
        }

        //uid：このIDで、どのメッセージについてのステータスか判定
        //ErrorCode: 送信したメッセージが正常におくられたか判定
        void SendStatusRecieved(string uid, int ErrorCode)
        {

        }

        void DeqMessageEvent(object sender, DeqMessageEventArgs e)
        {
            //if (this.InvokeRequired)
            //{
            //    this.Invoke((MethodInvoker)delegate { DeqMessageEvent(sender, e); });
            //    return;
            //}
        }

        private string NewGUID()
        {
            var guid = Guid.NewGuid();
            return guid.ToString();
        }

        private void MakeAndSendInitMessage()
        {
            IMessageParameters messageParameters = new Messages();

            messageParameters.Client = "Backend";
            messageParameters.Target = "RoutingModule";
            messageParameters.UID = NewGUID();
            messageParameters.Command = "Init";
            messageParameters.ControlType = "Mutation";

            SendMessage(messageParameters);
        }

        private void MakeAndSendQueryMessage()
        {
            IMessageParameters messageParameters = new Messages();

            messageParameters.Client = "Backend";
            messageParameters.Target = "SmartSoft";
            messageParameters.UID = NewGUID();
            messageParameters.Command = "Parameter";
            messageParameters.ControlType = "Query";

            //Xray
            IGroup groupX = new Group();
            groupX.GroupName = "XRay"; //Group name
            //パラメーター名を追加
            groupX.AddParameter("m_BeamVoltageInV", "");
            groupX.AddParameter("m_CondenserVoltageInV", "");
            groupX.AddParameter("m_BlankVoltageInV", "");
            groupX.AddParameter("m_ObjectiveCurrentInA", "");
            groupX.AddParameter("m_FilamentCurrentMaxInA", "");
            messageParameters.Datas.Groups.Add(groupX as Group);

            //IonGun
            IGroup groupI = new Group();
            groupI.GroupName = "IonGun"; //Group name
            //パラメーター名を追加
            groupI.AddParameter("m_BeamkV", "");
            groupI.AddParameter("m_GridV", "");
            groupI.AddParameter("m_FloatV", "");
            groupI.AddParameter("m_Condenser", "");
            groupI.AddParameter("m_Objective", "");
            groupI.AddParameter("m_BendVoltage", "");
            messageParameters.Datas.Groups.Add(groupI as Group);

            SendMessage(messageParameters);
        }

        private void MakeAndSendMutationMessage()
        {
            IMessageParameters messageParameters;

            messageControl.CreateMessageParams("SmartSoft", "Backend", "Parameter", "Mutation", out messageParameters);

            //Xray
            IGroup groupX = new Group();
            groupX.GroupName = "XRay"; //Group name
            //パラメーター名を追加
            groupX.AddParameter("m_BeamVoltageInV", "25000");
            groupX.AddParameter("m_CondenserVoltageInV", "900");
            groupX.AddParameter("m_BlankVoltageInV", "300");
            groupX.AddParameter("m_ObjectiveCurrentInA", "1.9");
            groupX.AddParameter("m_FilamentCurrentMaxInA", "3.8");
            messageParameters.Datas.Groups.Add(groupX as Group);

            //IonGun
            IGroup groupI = new Group();
            groupI.GroupName = "IonGun"; //Group name
            //パラメーター名を追加
            groupI.AddParameter("m_BeamkV", "4.0");
            groupI.AddParameter("m_GridV", "100");
            groupI.AddParameter("m_FloatV", "10");
            groupI.AddParameter("m_Condenser", "120");
            groupI.AddParameter("m_Objective", "900");
            groupI.AddParameter("m_BendVoltage", "230");
            messageParameters.Datas.Groups.Add(groupI as Group);

            SendMessage(messageParameters);
        }

        private int SendMessage(IMessageParameters messageParameters)
        {
            //この変換は、Listに表示するため
            string msg;
            int ec = messageControl.Serialize(messageParameters, out msg);

            //送信したメッセージのUIDを送る時に取得
            ec = messageControl.SendMessage(messageParameters, out SendUID);

            senderrorcode = ec;
            return ec;
        }

        [HttpGet(Name = "SmartSoftGet")]
        public string SmartSoftGet()
        {
            messageControl = TESTclass.messageControl;
            TESTclass.returnMessage = null;

            MakeAndSendQueryMessage();

            var source = new CancellationTokenSource();

            //タイムアウトの設定 10秒
            source.CancelAfter(10000);

            // SmartSoftからのメッセージの取得を待つ
            Task t = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    source.Token.ThrowIfCancellationRequested();
                    Thread.Sleep(100);//Task.Delay(100);//Delayが使えればDelayが良いな･･･。
                    if (TESTclass.returnMessage != null)
                    {
                        break;
                    }
                }
            }, source.Token);

            try
            {
                t.Wait(source.Token);//OperationCanceledExceptionが発生します。
                                     //t.Wait();//AggregateExceptionが発生します。
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("OperationCanceledExceptionが発生しました。");
            }
            catch (AggregateException)
            {
                Console.WriteLine("AggregateExceptionが発生しました。");
            }
            revmessage = TESTclass.returnMessage;

            return revmessage;
        }

        [HttpPost(Name = "SmartSoftSet")]
        public string SmartSoftSet()
        {
            try 
            {
                messageControl = TESTclass.messageControl;
                TESTclass.returnMessage = null;

                MakeAndSendMutationMessage();

                //TimeoutCheck();
                var source = new CancellationTokenSource();

                //タイムアウトの設定 10秒
                source.CancelAfter(10000);

                // SmartSoftからのメッセージの取得を待つ
                Task t = Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        source.Token.ThrowIfCancellationRequested();
                        Thread.Sleep(100);//Task.Delay(100);//Delayが使えればDelayが良いな･･･。
                        if (TESTclass.returnMessage != null)
                        {
                            break;
                        }
                    }
                }, source.Token);

                try
                {
                    t.Wait(source.Token);//OperationCanceledExceptionが発生します。
                                         //t.Wait();//AggregateExceptionが発生します。
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("OperationCanceledExceptionが発生しました。");
                }
                catch (AggregateException)
                {
                    Console.WriteLine("AggregateExceptionが発生しました。");
                }
            }
            catch (Exception ex)
            {
                // HTTPステータスコードを返す(仮)
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            revmessage = TESTclass.returnMessage;

            return revmessage;
        }
    }
}