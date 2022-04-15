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

        private void MakeAndSendAcqMessage()
        {
            IMessageParameters messageParameters;

            //TargetをSmartSoftにしていますが、テスト用でそうしています。
            //正規の場合は、TargetはSmartControlになります。
            // messageControl.CreateMessageParams("SmartControl", "Backend", "Acquisition", "Mutation", out messageParameters);

            messageControl.CreateMessageParams("SmartSoft", "Backend", "Queue", "Mutation", out messageParameters);

            //Queue　作成
            //例 Spectrum
            IQueueHelper queueHelper = new QueueHelper();

            string acqtype = "Spectrum";
            string acqtec = "XPS";

            ITaskHelper taskHelper = new TaskHelper(acqtec, acqtype);

            //Indexを設定。
            //Taskの名前がきまる
            taskHelper.SetupName(1);

            //実行するかどうか。falseの場合は、このタスクをスキップ
            taskHelper.Active = true;

            //TaskにJobを追加していく
            //JobTypeを引数としてAddすると、そのJobの設定名を自動で作成してOutでかえす

            //Position JOB
            AddPosition(queueHelper, taskHelper, acqtec, acqtype);

            //Acquisition Setup JOB
            AddAcqSetup(queueHelper, taskHelper, acqtec, acqtype);

            //Acquisition JOB
            AddAcq(queueHelper, taskHelper, acqtec, acqtype);

            //Taskの設定が終了
            //設定したJobからリストを作成
            taskHelper.EndUpdateJob();

            //QueueにTaskを追加
            queueHelper.AddTask(taskHelper.Task);

            //JSONに変換
            string json;
            int ec = queueHelper.ToJSONString(out json, false);

            //Groupを作成
            IGroup grp = new Group();
            //Group NameをAutoToolに設定
            grp.GroupName = "AutoTool";
            //ItemにQueueのJSONを設定
            grp.Item = json;

            //表示用に改行をいれて、変換
            string jsonview;
            ec = queueHelper.ToJSONString(out jsonview);
            //textBox6.Text = jsonview;

            //Datasに追加
            messageParameters.Datas.AddGroup(grp);

            //送信
            SendMessage(messageParameters);

            //測定メッセージのGUID
            //AcqSendGUID = SendUID;
            //このGUIDと同じ、ReturnDatasのUIDがEventで戻ってきたら測定が終了となります。
        }

        private void AddPosition(IQueueHelper queueHelper, ITaskHelper taskHelper, string acqtec, string acqtype)
        {
            string settingName = "";

            //Jobを追加
            taskHelper.AddJob(JobType.PositionList, out settingName);

            //位置の設定

            //Data(設定)作成用のHelperを作成
            //コンストラクタの引数に、測定手法、測定タイプを入れる
            IDataHelper dataHelper = new DataHelper(acqtec, acqtype);

            //設定名を設定
            dataHelper.DataName = settingName;

            IPositionHelper ph = new PositionHelper(); //Main
            IPositionHelper subph = new PositionHelper(); //Sub(SXIで測定位置を設定した場合に使用

            //Point 1
            //x, yは設定しなくても問題ない

            //写真上にPoint測定を追加した場合
            //UpperLeft(U,V) LowerRight(U,V)は、mmを使用
            //PointなのでUpperLeft(U,V) LowerRight(U,V)は同じ値
            ph.SetupPosition("Point", 0, 0, 15.65, 45.0, 230.0, 5.9863, -1.0795, 5.9863, -1.0795);

            //CommentやDefect Nameを設定する場合
            // ph.DefectName = "P1";
            // ph.Comment = "For Test";

            //設定した値からItem Listを作成する
            ph.BuildPositionItems();

            //Data Listに追加する
            dataHelper.AddPositionItems(ph.PositionItems);


            /*
            //SXIを使用する場合
            
            ph.SetupPosition("SXI", 0, 0, 15.65, 45.0, 230.0, -1.0796, 0.3922, -0.0796, -0.6078);
            ph.ImageFilename = @"C:\Datafiles\2021\12\tmp.127.sxi"; //SXI File path

            //設定した値からItem Listを作成する
            ph.BuildPositionItems();

            //Data Listに追加する (Main Position)
            dataHelper.AddPositionItems(ph.PositionItems);

            //SXI上のポイントの設定を行う
            //Point 1
            //UpperLeft(U,V) LowerRight(U,V)は、μmを使用
            subph.SetupPosition("Point", 0, 0, 15.65, 45.0, 230.0, 220.3389, -296.6102, 220.3390, -296.6102, false);

            //設定した値からItem Listを作成する
            subph.BuildPositionItems();

            //Data Listに追加する(Sub position)
            dataHelper.AddSubPositionItems(ph.GUID, ph.ImageFilename, ph.DefectType, subph.PositionItems);

            //Point 2
            //UpperLeft(U,V) LowerRight(U,V)は、μmを使用
            subph.SetupPosition("Point", 0, 0, 15.65, 45.0, 230.0, 290.3389, -296.6102, 290.3390, -296.6102, false);

            //設定した値からItem Listを作成する
            subph.BuildPositionItems();

            //Data Listに追加する(Sub position)
            dataHelper.AddSubPositionItems(ph.GUID, ph.ImageFilename, ph.DefectType, subph.PositionItems);
            */


            //QueueにDataを追加
            queueHelper.AddData(dataHelper.DataParam);
        }

        private void AddAcqSetup(IQueueHelper queueHelper, ITaskHelper taskHelper, string acqtec, string acqtype)
        {
            string settingName = "";

            //Jobを追加
            taskHelper.AddJob(JobType.AcqSetup, out settingName);

            //Acquisition Setup

            //Data(設定)作成用のHelperを作成
            //コンストラクタの引数に、測定手法、測定タイプを入れる
            IDataHelper dataHelper = new DataHelper(acqtec, acqtype);

            //設定名を設定
            dataHelper.DataName = settingName;

            ParameterDictionaryHelper parameterDictionaryHelper = new ParameterDictionaryHelper();

            //測定中に中和銃を用いた中和を行う場合
            parameterDictionaryHelper.AddParameters("m_AutoEGunNeutActive", "Auto [A]"); //Offの場合は、値を"Off"にする
            //その時に、使用する中和銃の設定を設定する
            parameterDictionaryHelper.AddParameters("m_EGunNeutSetting", "PREVIOUS");

            //測定中にイオン銃も用いた中和を行う場合
            parameterDictionaryHelper.AddParameters("m_AutoIonGunNeutActive", "Auto [A]"); //Offの場合は、値を"Off"にする
            //その時に、使用する中和銃の設定を設定する
            parameterDictionaryHelper.AddParameters("m_IonGunNeutSetting", "500V3x3");

            dataHelper.SetToParameterList(parameterDictionaryHelper.GetParamDict());

            //QueueにDataを追加
            queueHelper.AddData(dataHelper.DataParam);
        }

        private void AddAcq(IQueueHelper queueHelper, ITaskHelper taskHelper, string acqtec, string acqtype)
        {
            string settingName = "";

            //Jobを追加
            taskHelper.AddJob(JobType.Acquisition, out settingName);

            //Acquisition

            //Data(設定)作成用のHelperを作成
            //コンストラクタの引数に、測定手法、測定タイプを入れる
            IDataHelper dataHelper = new DataHelper(acqtec, acqtype);

            //設定名を設定
            dataHelper.DataName = settingName;


            IRegionHelper rh = new RegionHelper();

            //測定の共通設定を設定
            //FAT Mode = true, FRR Mode = false
            //Scanned = true, Unscanned = false
            //サイクル数
            //Time/Step [msec] : 1点の測定時間
            //Source type
            //Sourceの設定
            rh.SetupCommon(true, true, 2, 20.0, "FXS", "100u25W15KV");

            int region_errorcode;
            //一つ目のRegion情報を設定
            //ここで、共通設定のFAT/FRR, Scanned/Unscannedと異なる関数で設定するとエラーコードが返る

            //基本的に、
            //loweranalは、loweracq+1
            //upperanalは、upperacq-1
            region_errorcode = rh.SetupForXPSScan("O1s", 8, 5, 523.0, 543.0, 524.0, 542.0, 14.0, 0.05);
            //Errorの場合
            //region_errorcode = rh.SetupForXPSUnscan("C1s", 6, 1, 285.0, 31.0, 28.0, 0.1);
            //region_errorcodeに0以外になり設定はされない
            if (region_errorcode == 0)
            {
                //設定した値からItem Listを作成する
                rh.BuildRegionItems();

                //Data Listに追加する
                dataHelper.AddToRegionList(rh.RegionItems);
            }


            //二つ目のRegion情報を設定
            region_errorcode = rh.SetupForXPSScan("C1s", 6, 5, 278.0, 298.0, 279.0, 297.0, 28.0, 0.1);

            if (region_errorcode == 0)
            {
                //設定した値からItem Listを作成する
                rh.BuildRegionItems();

                //Data Listに追加する
                dataHelper.AddToRegionList(rh.RegionItems);
            }

            //更に追加する場合は、上と同じ流れでおこなう。


            //共通設定：設定した値からItem Listを作成する
            rh.BuidCommonItems();

            //測定タイプがDepthProfileの場合
            if (acqtype == "Profile")
            {
                IDepthSettingHelper dsh = new DepthSettingHelper();

                dsh.Clear();

                //Alternate/Continuousの設定をおこなう
                //Continuousの場合の設定は、↓に記載している
                dsh.Alternate = true;
                //Zalarの設定
                dsh.Zalar = false;

                //Layer 1
                //Layer名、Gun Type, setting名, サイクル数, Interval(min), Dual Sputter?
                //Dual SputterはGun TypeがArの場合は無効
                dsh.SetAlternate("Layer1", "Ar", "1KV3x3", 5, 1.0, false);

                //ビルド
                dsh.BuildAlternateLayerItems();

                //Depth Profile Listに追加
                dataHelper.AddToDepthProfileList(dsh.AlternateLayerItems);

                //Layer 2
                dsh.SetAlternate("Layer2", "C60", "1KV3x3", 10, 1.0, true);

                dsh.BuildAlternateLayerItems();

                dataHelper.AddToDepthProfileList(dsh.AlternateLayerItems);

                //Dual sputter用Ar設定
                //dual sputterを行わない場合は、空白でもOK
                dsh.DualArSetting = "500V3x3";

                //Common
                dsh.SetCommonToDict(rh.CommonItems);

                //Continuousの場合
                /*
                dsh.Alternate = false;
                dsh.Zalar = false;

                dsh.SetContinuous("Ar", "1KV3x3", 30, false);

                dsh.SetContinuoutToDict(rh.CommonItems);

                dsh.SetCommonToDict(rh.CommonItems);

                */
            }
            //測定タイプがAngleProfileの場合
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

                //Angle 120deg　範囲外のエラーがかえる
                //angleerror = ash.SetAngle(5, 120.0);

                //Common
                ash.SetCommonToDict(rh.CommonItems);
            }

            //共通設定をセット
            dataHelper.SetToParameterList(rh.CommonItems);

            //QueueにDataを追加
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

            //タイムアウトの設定 10秒
            source.CancelAfter(10000);

            // SmartSoftからのメッセージの取得を待つ
            System.Threading.Tasks.Task t = System.Threading.Tasks.Task.Factory.StartNew(() =>
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
    }
}