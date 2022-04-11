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
            groupX.AddParameter("m_BeamVoltageInV", "25000");
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

            //���M�������b�Z�[�W��UID�𑗂鎞�Ɏ擾
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

            //�^�C���A�E�g�̐ݒ� 10�b
            source.CancelAfter(10000);

            // SmartSoft����̃��b�Z�[�W�̎擾��҂�
            Task t = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    source.Token.ThrowIfCancellationRequested();
                    Thread.Sleep(100);//Task.Delay(100);//Delay���g�����Delay���ǂ��ȥ���B
                    if (TESTclass.returnMessage != null)
                    {
                        break;
                    }
                }
            }, source.Token);

            try
            {
                t.Wait(source.Token);//OperationCanceledException���������܂��B
                                     //t.Wait();//AggregateException���������܂��B
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("OperationCanceledException���������܂����B");
            }
            catch (AggregateException)
            {
                Console.WriteLine("AggregateException���������܂����B");
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

                //�^�C���A�E�g�̐ݒ� 10�b
                source.CancelAfter(10000);

                // SmartSoft����̃��b�Z�[�W�̎擾��҂�
                Task t = Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        source.Token.ThrowIfCancellationRequested();
                        Thread.Sleep(100);//Task.Delay(100);//Delay���g�����Delay���ǂ��ȥ���B
                        if (TESTclass.returnMessage != null)
                        {
                            break;
                        }
                    }
                }, source.Token);

                try
                {
                    t.Wait(source.Token);//OperationCanceledException���������܂��B
                                         //t.Wait();//AggregateException���������܂��B
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("OperationCanceledException���������܂����B");
                }
                catch (AggregateException)
                {
                    Console.WriteLine("AggregateException���������܂����B");
                }
            }
            catch (Exception ex)
            {
                // HTTP�X�e�[�^�X�R�[�h��Ԃ�(��)
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            revmessage = TESTclass.returnMessage;

            return revmessage;
        }
    }
}