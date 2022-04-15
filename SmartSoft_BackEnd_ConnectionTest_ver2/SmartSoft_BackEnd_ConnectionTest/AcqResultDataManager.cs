using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UPMessageControlCore;

namespace SmartSoft_BackEnd_ConnectionTest
{
    internal class AcqResultDataManager
    {
        private static Lazy<AcqResultDataManager> instance = new Lazy<AcqResultDataManager>();
        public static AcqResultDataManager Instance => instance.Value;


        public string TaskID { get; private set; } = "";
        public int NumOfRegions { get; private set; } = 0;
        public string AcqType { get; private set; } = "";

        public Dictionary<string, ResultItem> ResultList = new Dictionary<string, ResultItem>();

        public event EventHandler<AcqHeaderEventArgs>? UpdateAcqHeader;
        public event EventHandler<ResultItem>? UpdateAcqResult;
        //ResultItem

        public void Clear()
        {
            ResultList.Clear();

            TaskID = "";
            NumOfRegions = 0;
            AcqType = "";
        }

        private void SetHeader(IAcqHeader acqheader)
        {
            bool FirstUpdateFlag = false;
            if (TaskID == "")
            {
                FirstUpdateFlag = true;
            }

            TaskID = acqheader.TaskID;
            NumOfRegions = acqheader.NumOfRegions;
            AcqType = acqheader.AcquisitionType;

            ResultItem ritem;
            for (int i = 0; i < NumOfRegions; i++)
            {
                if (!ResultList.ContainsKey((i + 1).ToString()))
                {
                    ritem = new ResultItem();
                    ResultList.Add((i + 1).ToString(), ritem);
                }   
            }

            if (FirstUpdateFlag)
            {
                if (UpdateAcqHeader != null)
                {
                    AcqHeaderEventArgs ah = new AcqHeaderEventArgs(acqheader);
                    UpdateAcqHeader(this, ah);
                }
            }
        }

        public void SetData(IDataInfo datainfo)
        {
            SetHeader(datainfo.AcqHeader);

            int didx = 0;
            IAcqData acqData;
            ResultItem ritem;
            for (int i = 0; i < NumOfRegions; i++)
            {
                if (!ResultList.TryGetValue((i+1).ToString(), out ritem))
                {
                    ritem = new ResultItem();
                    ResultList.Add((i + 1).ToString(), ritem);
                }

                List<AcqData>? acqdatalist;
                if (datainfo.AcqDataList.TryGetValue((i + 1).ToString(), out acqdatalist))
                {
                    //データ取得
                    int numdatas = acqdatalist.Count;
                    for (int j = 0; j < numdatas; j++)
                    {
                        acqData = acqdatalist[j];
                        if (String.Compare(acqData.DataType, "Spectrum", true) == 0)
                        {
                            //Spectrum
                            ritem.Name = acqData.Region;
                            ritem.CurrentCycle = acqData.CurrentCycle;
                            ritem.NoPoint = acqData.TotalDatas;

                            int dnum = acqData.XDatas.Count;
                            int xnum = ritem.X.Count;
                            for (int k = 0; k < dnum; k++)
                            {
                                if (xnum <= k)
                                    ritem.X.Add(acqData.XDatas[k]);
                                else
                                    ritem.X[k] = acqData.XDatas[k];
                            }

                            int idx = 0;
                            int ynum = ritem.Y.Count;
                            foreach (KeyValuePair<string, List<double>> kvp in acqData.YDatas)
                            {
                                if (ynum <= idx)
                                {
                                    ritem.Y.Add(kvp.Value);
                                }
                                else
                                {
                                    ritem.Y[idx] = kvp.Value;
                                }
                                idx++;
                            }
                            

                        }
                        else if (String.Compare(acqData.DataType, "Intensity", true) == 0)
                        {

                        }
                        
                    }

                    if (UpdateAcqResult != null)
                    {
                        UpdateAcqResult(this, ritem);
                    }
                }
            }
        }


        

    }

    public class ResultItem
    {
        public string Name = "";
        public int AtomicNo = 0;
        public int NoPoint = 0;
        public int CurrentCycle = 0;

        public List<double> X = new List<double>();
        public List<List<double>> Y = new List<List<double>>();

        public List<double> IntensityX = new List<double>();
        public List<List<double>> IntensityY = new List<List<double>>();
    }
}
