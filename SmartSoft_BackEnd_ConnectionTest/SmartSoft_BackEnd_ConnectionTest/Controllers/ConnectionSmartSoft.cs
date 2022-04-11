using Microsoft.AspNetCore.Mvc;
using System.Net;
using UPMessageControlCore;

namespace SmartSoft_BackEnd_ConnectionTest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConnectionSmartSoft : ControllerBase
    {
        IMessageControl messageControl;
        private bool ConnectFlag = false;
        private int errorcode = 0;
        private int senderrorcode = 0;
        private int Reverrorcode = 0;
        private string errormsg = "";
        private string SendUID = "";

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

        //Messageを受信して、Deserialize後に発生するイベント
        //MessageRecievedの後に発生
        void MessageDeserialized(IMessageParameters? MessageParam, string Message, int ErrorCode)
        {
            //DeserializeされたParameterがイベントでここにきます

            //受信したメッセージをJSONから変換できなかった場合に、ErrorCodeが0以外になる

            Reverrorcode = ErrorCode;

            if (ErrorCode == 0)
            {
                //変換OK

                //SmartSoft、SmartControlでエラーが発生した場合は、ここでMessageParamから判定する
                // MessageParam.Datas.RetunrData.ErrorCode : エラーコード
                // MessageParam.Datas.RetunrData.UID : 送信したメッセージのID。このIDでどのメッセージの戻りEventか判定

                Reverrorcode = MessageParam.Datas.RetunrData.ErrorCode;
            }
            else
            {
                //変換しっぱい
                //この場合、MessageParamはnull
            }

            MessageManager.Instance.AddMessageParam(Message, MessageParam);

            // メッセージを保持
            TESTclass.returnMessage = Message;
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
            groupX.AddParameter("m_BeamVoltageInV", "20000");
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
            //listBox1.Items.Add(msg);

            //送信したメッセージのUIDを送る時に取得
            ec = messageControl.SendMessage(messageParameters, out SendUID);

            senderrorcode = ec;
            return ec;
        }

        [HttpPost(Name = "Connection")]
        public void Connection()
        {
            try
            {
                messageControl = TESTclass.messageControl;

                //Event
                //通信の状態イベント
                (messageControl as UPMessageCtrlFunc).ConnectionChanged += ConnectionChanged;
                //Send後に送信先から、正常におくれたかのステータスが戻る時のイベント
                (messageControl as UPMessageCtrlFunc).SendStatusRecieved += SendStatusRecieved;
                //Messageの受信=>Deserialize後のイベント
                (messageControl as UPMessageCtrlFunc).MessageDeserialized += MessageDeserialized;

                //Message用のqueue
                MessageManager.Instance.initialize();
                MessageManager.Instance.GetMessageParams += DeqMessageEvent;

                IPHostEntry ip = Dns.GetHostEntry("");
                string ipa = ip.AddressList[0].ToString();

                int PortNo = Convert.ToInt32("1100"); //テストソフトのポート番号は、1100

                int ec = messageControl.Connect(ipa, PortNo);

                // Connectを試行してエラーコードが0以外の場合はエラー
                if (ec == 0)
                {
                    MakeAndSendInitMessage();
                }
                else
                {
                    // HTTPステータスコードを返す(仮)
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                }
            }
            catch (Exception ex)
            {
                // HTTPステータスコードを返す(仮)
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }

        }
    }
}