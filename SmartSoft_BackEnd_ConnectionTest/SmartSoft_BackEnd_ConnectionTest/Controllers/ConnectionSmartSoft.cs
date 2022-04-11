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

        //�ʐM�̏�Ԃ��ς�����ꍇ�ɔ�������C�x���g
        void ConnectionChanged(bool Connected, int ECode, string EMessage)
        {
            ConnectFlag = Connected;
            errorcode = ECode;
            errormsg = EMessage;
        }

        //uid�F����ID�ŁA�ǂ̃��b�Z�[�W�ɂ��ẴX�e�[�^�X������
        //ErrorCode: ���M�������b�Z�[�W������ɂ�����ꂽ������
        void SendStatusRecieved(string uid, int ErrorCode)
        {

        }

        //Message����M���āADeserialize��ɔ�������C�x���g
        //MessageRecieved�̌�ɔ���
        void MessageDeserialized(IMessageParameters? MessageParam, string Message, int ErrorCode)
        {
            //Deserialize���ꂽParameter���C�x���g�ł����ɂ��܂�

            //��M�������b�Z�[�W��JSON����ϊ��ł��Ȃ������ꍇ�ɁAErrorCode��0�ȊO�ɂȂ�

            Reverrorcode = ErrorCode;

            if (ErrorCode == 0)
            {
                //�ϊ�OK

                //SmartSoft�ASmartControl�ŃG���[�����������ꍇ�́A������MessageParam���画�肷��
                // MessageParam.Datas.RetunrData.ErrorCode : �G���[�R�[�h
                // MessageParam.Datas.RetunrData.UID : ���M�������b�Z�[�W��ID�B����ID�łǂ̃��b�Z�[�W�̖߂�Event������

                Reverrorcode = MessageParam.Datas.RetunrData.ErrorCode;
            }
            else
            {
                //�ϊ������ς�
                //���̏ꍇ�AMessageParam��null
            }

            MessageManager.Instance.AddMessageParam(Message, MessageParam);

            // ���b�Z�[�W��ێ�
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
            //�p�����[�^�[����ǉ�
            groupX.AddParameter("m_BeamVoltageInV", "");
            groupX.AddParameter("m_CondenserVoltageInV", "");
            groupX.AddParameter("m_BlankVoltageInV", "");
            groupX.AddParameter("m_ObjectiveCurrentInA", "");
            groupX.AddParameter("m_FilamentCurrentMaxInA", "");
            messageParameters.Datas.Groups.Add(groupX as Group);

            //IonGun
            IGroup groupI = new Group();
            groupI.GroupName = "IonGun"; //Group name
            //�p�����[�^�[����ǉ�
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
            //�p�����[�^�[����ǉ�
            groupX.AddParameter("m_BeamVoltageInV", "20000");
            groupX.AddParameter("m_CondenserVoltageInV", "900");
            groupX.AddParameter("m_BlankVoltageInV", "300");
            groupX.AddParameter("m_ObjectiveCurrentInA", "1.9");
            groupX.AddParameter("m_FilamentCurrentMaxInA", "3.8");
            messageParameters.Datas.Groups.Add(groupX as Group);

            //IonGun
            IGroup groupI = new Group();
            groupI.GroupName = "IonGun"; //Group name
            //�p�����[�^�[����ǉ�
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
            //���̕ϊ��́AList�ɕ\�����邽��
            string msg;
            int ec = messageControl.Serialize(messageParameters, out msg);
            //listBox1.Items.Add(msg);

            //���M�������b�Z�[�W��UID�𑗂鎞�Ɏ擾
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
                //�ʐM�̏�ԃC�x���g
                (messageControl as UPMessageCtrlFunc).ConnectionChanged += ConnectionChanged;
                //Send��ɑ��M�悩��A����ɂ����ꂽ���̃X�e�[�^�X���߂鎞�̃C�x���g
                (messageControl as UPMessageCtrlFunc).SendStatusRecieved += SendStatusRecieved;
                //Message�̎�M=>Deserialize��̃C�x���g
                (messageControl as UPMessageCtrlFunc).MessageDeserialized += MessageDeserialized;

                //Message�p��queue
                MessageManager.Instance.initialize();
                MessageManager.Instance.GetMessageParams += DeqMessageEvent;

                IPHostEntry ip = Dns.GetHostEntry("");
                string ipa = ip.AddressList[0].ToString();

                int PortNo = Convert.ToInt32("1100"); //�e�X�g�\�t�g�̃|�[�g�ԍ��́A1100

                int ec = messageControl.Connect(ipa, PortNo);

                // Connect�����s���ăG���[�R�[�h��0�ȊO�̏ꍇ�̓G���[
                if (ec == 0)
                {
                    MakeAndSendInitMessage();
                }
                else
                {
                    // HTTP�X�e�[�^�X�R�[�h��Ԃ�(��)
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                }
            }
            catch (Exception ex)
            {
                // HTTP�X�e�[�^�X�R�[�h��Ԃ�(��)
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }

        }
    }
}