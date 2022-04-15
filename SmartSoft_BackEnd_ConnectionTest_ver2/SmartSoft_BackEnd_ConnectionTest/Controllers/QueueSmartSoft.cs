using Microsoft.AspNetCore.Mvc;
using QueueSerializerCore;
using System.Net;
using UPMessageControlCore;

namespace SmartSoft_BackEnd_ConnectionTest.Controllers
{
    [Route("[controller]")]
    public class QueueSmartSoft : ControllerBase
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

        private void MakeAndSendAcqMessage()
        {
            IMessageParameters messageParameters;

            //Target��SmartSoft�ɂ��Ă��܂����A�e�X�g�p�ł������Ă��܂��B
            //���K�̏ꍇ�́ATarget��SmartControl�ɂȂ�܂��B
            // messageControl.CreateMessageParams("SmartControl", "Backend", "Acquisition", "Mutation", out messageParameters);

            messageControl.CreateMessageParams("SmartSoft", "Backend", "Queue", "Mutation", out messageParameters);

            //Queue�@�쐬
            //�� Spectrum
            IQueueHelper queueHelper = new QueueHelper();

            string acqtype = "Spectrum";
            string acqtec = "XPS";

            ITaskHelper taskHelper = new TaskHelper(acqtec, acqtype);

            //Index��ݒ�B
            //Task�̖��O�����܂�
            taskHelper.SetupName(1);

            //���s���邩�ǂ����Bfalse�̏ꍇ�́A���̃^�X�N���X�L�b�v
            taskHelper.Active = true;

            //Task��Job��ǉ����Ă���
            //JobType�������Ƃ���Add����ƁA����Job�̐ݒ薼�������ō쐬����Out�ł�����

            //Position JOB
            AddPosition(queueHelper, taskHelper, acqtec, acqtype);

            //Acquisition Setup JOB
            AddAcqSetup(queueHelper, taskHelper, acqtec, acqtype);

            //Acquisition JOB
            AddAcq(queueHelper, taskHelper, acqtec, acqtype);

            //Task�̐ݒ肪�I��
            //�ݒ肵��Job���烊�X�g���쐬
            taskHelper.EndUpdateJob();

            //Queue��Task��ǉ�
            queueHelper.AddTask(taskHelper.Task);

            //JSON�ɕϊ�
            string json;
            int ec = queueHelper.ToJSONString(out json, false);

            //Group���쐬
            IGroup grp = new Group();
            //Group Name��AutoTool�ɐݒ�
            grp.GroupName = "AutoTool";
            //Item��Queue��JSON��ݒ�
            grp.Item = json;

            //�\���p�ɉ��s������āA�ϊ�
            string jsonview;
            ec = queueHelper.ToJSONString(out jsonview);
            //textBox6.Text = jsonview;

            //Datas�ɒǉ�
            messageParameters.Datas.AddGroup(grp);

            //���M
            SendMessage(messageParameters);

            //���胁�b�Z�[�W��GUID
            //AcqSendGUID = SendUID;
            //����GUID�Ɠ����AReturnDatas��UID��Event�Ŗ߂��Ă����瑪�肪�I���ƂȂ�܂��B
        }

        private void AddPosition(IQueueHelper queueHelper, ITaskHelper taskHelper, string acqtec, string acqtype)
        {
            string settingName = "";

            //Job��ǉ�
            taskHelper.AddJob(JobType.PositionList, out settingName);

            //�ʒu�̐ݒ�

            //Data(�ݒ�)�쐬�p��Helper���쐬
            //�R���X�g���N�^�̈����ɁA�����@�A����^�C�v������
            IDataHelper dataHelper = new DataHelper(acqtec, acqtype);

            //�ݒ薼��ݒ�
            dataHelper.DataName = settingName;

            IPositionHelper ph = new PositionHelper(); //Main
            IPositionHelper subph = new PositionHelper(); //Sub(SXI�ő���ʒu��ݒ肵���ꍇ�Ɏg�p

            //Point 1
            //x, y�͐ݒ肵�Ȃ��Ă����Ȃ�

            //�ʐ^���Point�����ǉ������ꍇ
            //UpperLeft(U,V) LowerRight(U,V)�́Amm���g�p
            //Point�Ȃ̂�UpperLeft(U,V) LowerRight(U,V)�͓����l
            ph.SetupPosition("Point", 0, 0, 15.65, 45.0, 230.0, 5.9863, -1.0795, 5.9863, -1.0795);

            //Comment��Defect Name��ݒ肷��ꍇ
            // ph.DefectName = "P1";
            // ph.Comment = "For Test";

            //�ݒ肵���l����Item List���쐬����
            ph.BuildPositionItems();

            //Data List�ɒǉ�����
            dataHelper.AddPositionItems(ph.PositionItems);


            /*
            //SXI���g�p����ꍇ
            
            ph.SetupPosition("SXI", 0, 0, 15.65, 45.0, 230.0, -1.0796, 0.3922, -0.0796, -0.6078);
            ph.ImageFilename = @"C:\Datafiles\2021\12\tmp.127.sxi"; //SXI File path

            //�ݒ肵���l����Item List���쐬����
            ph.BuildPositionItems();

            //Data List�ɒǉ����� (Main Position)
            dataHelper.AddPositionItems(ph.PositionItems);

            //SXI��̃|�C���g�̐ݒ���s��
            //Point 1
            //UpperLeft(U,V) LowerRight(U,V)�́A��m���g�p
            subph.SetupPosition("Point", 0, 0, 15.65, 45.0, 230.0, 220.3389, -296.6102, 220.3390, -296.6102, false);

            //�ݒ肵���l����Item List���쐬����
            subph.BuildPositionItems();

            //Data List�ɒǉ�����(Sub position)
            dataHelper.AddSubPositionItems(ph.GUID, ph.ImageFilename, ph.DefectType, subph.PositionItems);

            //Point 2
            //UpperLeft(U,V) LowerRight(U,V)�́A��m���g�p
            subph.SetupPosition("Point", 0, 0, 15.65, 45.0, 230.0, 290.3389, -296.6102, 290.3390, -296.6102, false);

            //�ݒ肵���l����Item List���쐬����
            subph.BuildPositionItems();

            //Data List�ɒǉ�����(Sub position)
            dataHelper.AddSubPositionItems(ph.GUID, ph.ImageFilename, ph.DefectType, subph.PositionItems);
            */


            //Queue��Data��ǉ�
            queueHelper.AddData(dataHelper.DataParam);
        }

        private void AddAcqSetup(IQueueHelper queueHelper, ITaskHelper taskHelper, string acqtec, string acqtype)
        {
            string settingName = "";

            //Job��ǉ�
            taskHelper.AddJob(JobType.AcqSetup, out settingName);

            //Acquisition Setup

            //Data(�ݒ�)�쐬�p��Helper���쐬
            //�R���X�g���N�^�̈����ɁA�����@�A����^�C�v������
            IDataHelper dataHelper = new DataHelper(acqtec, acqtype);

            //�ݒ薼��ݒ�
            dataHelper.DataName = settingName;

            ParameterDictionaryHelper parameterDictionaryHelper = new ParameterDictionaryHelper();

            //���蒆�ɒ��a�e��p�������a���s���ꍇ
            parameterDictionaryHelper.AddParameters("m_AutoEGunNeutActive", "Auto [A]"); //Off�̏ꍇ�́A�l��"Off"�ɂ���
            //���̎��ɁA�g�p���钆�a�e�̐ݒ��ݒ肷��
            parameterDictionaryHelper.AddParameters("m_EGunNeutSetting", "PREVIOUS");

            //���蒆�ɃC�I���e���p�������a���s���ꍇ
            parameterDictionaryHelper.AddParameters("m_AutoIonGunNeutActive", "Auto [A]"); //Off�̏ꍇ�́A�l��"Off"�ɂ���
            //���̎��ɁA�g�p���钆�a�e�̐ݒ��ݒ肷��
            parameterDictionaryHelper.AddParameters("m_IonGunNeutSetting", "500V3x3");

            dataHelper.SetToParameterList(parameterDictionaryHelper.GetParamDict());

            //Queue��Data��ǉ�
            queueHelper.AddData(dataHelper.DataParam);
        }

        private void AddAcq(IQueueHelper queueHelper, ITaskHelper taskHelper, string acqtec, string acqtype)
        {
            string settingName = "";

            //Job��ǉ�
            taskHelper.AddJob(JobType.Acquisition, out settingName);

            //Acquisition

            //Data(�ݒ�)�쐬�p��Helper���쐬
            //�R���X�g���N�^�̈����ɁA�����@�A����^�C�v������
            IDataHelper dataHelper = new DataHelper(acqtec, acqtype);

            //�ݒ薼��ݒ�
            dataHelper.DataName = settingName;


            IRegionHelper rh = new RegionHelper();

            //����̋��ʐݒ��ݒ�
            //FAT Mode = true, FRR Mode = false
            //Scanned = true, Unscanned = false
            //�T�C�N����
            //Time/Step [msec] : 1�_�̑��莞��
            //Source type
            //Source�̐ݒ�
            rh.SetupCommon(true, true, 2, 20.0, "FXS", "100u25W15KV");

            int region_errorcode;
            //��ڂ�Region����ݒ�
            //�����ŁA���ʐݒ��FAT/FRR, Scanned/Unscanned�ƈقȂ�֐��Őݒ肷��ƃG���[�R�[�h���Ԃ�

            //��{�I�ɁA
            //loweranal�́Aloweracq+1
            //upperanal�́Aupperacq-1
            region_errorcode = rh.SetupForXPSScan("O1s", 8, 5, 523.0, 543.0, 524.0, 542.0, 14.0, 0.05);
            //Error�̏ꍇ
            //region_errorcode = rh.SetupForXPSUnscan("C1s", 6, 1, 285.0, 31.0, 28.0, 0.1);
            //region_errorcode��0�ȊO�ɂȂ�ݒ�͂���Ȃ�
            if (region_errorcode == 0)
            {
                //�ݒ肵���l����Item List���쐬����
                rh.BuildRegionItems();

                //Data List�ɒǉ�����
                dataHelper.AddToRegionList(rh.RegionItems);
            }


            //��ڂ�Region����ݒ�
            region_errorcode = rh.SetupForXPSScan("C1s", 6, 5, 278.0, 298.0, 279.0, 297.0, 28.0, 0.1);

            if (region_errorcode == 0)
            {
                //�ݒ肵���l����Item List���쐬����
                rh.BuildRegionItems();

                //Data List�ɒǉ�����
                dataHelper.AddToRegionList(rh.RegionItems);
            }

            //�X�ɒǉ�����ꍇ�́A��Ɠ�������ł����Ȃ��B


            //���ʐݒ�F�ݒ肵���l����Item List���쐬����
            rh.BuidCommonItems();

            //����^�C�v��DepthProfile�̏ꍇ
            if (acqtype == "Profile")
            {
                IDepthSettingHelper dsh = new DepthSettingHelper();

                dsh.Clear();

                //Alternate/Continuous�̐ݒ�������Ȃ�
                //Continuous�̏ꍇ�̐ݒ�́A���ɋL�ڂ��Ă���
                dsh.Alternate = true;
                //Zalar�̐ݒ�
                dsh.Zalar = false;

                //Layer 1
                //Layer���AGun Type, setting��, �T�C�N����, Interval(min), Dual Sputter?
                //Dual Sputter��Gun Type��Ar�̏ꍇ�͖���
                dsh.SetAlternate("Layer1", "Ar", "1KV3x3", 5, 1.0, false);

                //�r���h
                dsh.BuildAlternateLayerItems();

                //Depth Profile List�ɒǉ�
                dataHelper.AddToDepthProfileList(dsh.AlternateLayerItems);

                //Layer 2
                dsh.SetAlternate("Layer2", "C60", "1KV3x3", 10, 1.0, true);

                dsh.BuildAlternateLayerItems();

                dataHelper.AddToDepthProfileList(dsh.AlternateLayerItems);

                //Dual sputter�pAr�ݒ�
                //dual sputter���s��Ȃ��ꍇ�́A�󔒂ł�OK
                dsh.DualArSetting = "500V3x3";

                //Common
                dsh.SetCommonToDict(rh.CommonItems);

                //Continuous�̏ꍇ
                /*
                dsh.Alternate = false;
                dsh.Zalar = false;

                dsh.SetContinuous("Ar", "1KV3x3", 30, false);

                dsh.SetContinuoutToDict(rh.CommonItems);

                dsh.SetCommonToDict(rh.CommonItems);

                */
            }
            //����^�C�v��AngleProfile�̏ꍇ
            else if (acqtype == "Angle")
            {
                IAngleSettingHelper ash = new AngleSettingHelper();

                ash.Clear();

                int angleerror = 0;
                //Layer 1
                ash.SetAngle(5, 10.0);

                ash.BuildAngleLayerItems();

                dataHelper.AddToAngleProfileList(ash.AngleLayerItems);

                //Layer 2
                ash.SetAngle(5, 45.0);

                ash.BuildAngleLayerItems();

                dataHelper.AddToAngleProfileList(ash.AngleLayerItems);

                //Layer 3
                ash.SetAngle(5, 70.0);

                ash.BuildAngleLayerItems();

                dataHelper.AddToAngleProfileList(ash.AngleLayerItems);

                //Angle 120deg�@�͈͊O�̃G���[��������
                //angleerror = ash.SetAngle(5, 120.0);

                //Common
                ash.SetCommonToDict(rh.CommonItems);
            }

            //���ʐݒ���Z�b�g
            dataHelper.SetToParameterList(rh.CommonItems);

            //Queue��Data��ǉ�
            queueHelper.AddData(dataHelper.DataParam);
        }

        [HttpPost(Name = "SmartSoftQueue")]
        public string SmartSoftGet()
        {
            messageControl = TESTclass.messageControl;
            TESTclass.returnMessage = null;

            AcqResultDataManager.Instance.Clear();

            //StatusList.Clear();

            MakeAndSendAcqMessage();

            var source = new CancellationTokenSource();

            //�^�C���A�E�g�̐ݒ� 10�b
            source.CancelAfter(10000);

            // SmartSoft����̃��b�Z�[�W�̎擾��҂�
            System.Threading.Tasks.Task t = System.Threading.Tasks.Task.Factory.StartNew(() =>
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
    }
}