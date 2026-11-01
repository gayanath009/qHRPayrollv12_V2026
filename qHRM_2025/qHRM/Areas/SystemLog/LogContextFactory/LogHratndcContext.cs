using qHRM.Areas.SystemLog.Models;
using qHRM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.SystemLog.LogContextFactory
{
    public class LogHratndcContext : ILogModelContext
    {
        private qHRMasterContext _hrmContext;
        public LogHratndcContext(qHRMasterContext hrmContext)
        {
            _hrmContext = hrmContext;
        }

        public int insert(int tableId, int recId, int userId)
        {
            throw new NotImplementedException();
        }

        public int update<T>(int tableId, int recId, T newModel, T existModel, int userId)
        {
            try
            {
                var newModelType = newModel.GetType();
                var extModelType = existModel.GetType();
                var ssn = Convert.ToInt64(DateTime.Now.ToString("yyyyMMddhhmmssfff"));
                var empSysId = (int)existModel.GetType().GetProperty("EmpSysId").GetValue(existModel, null);
                var atndcDate = (DateTime)existModel.GetType().GetProperty("AtndcDate").GetValue(existModel, null);
                var sftId = (int)existModel.GetType().GetProperty("SftId").GetValue(existModel, null); ;

                var newModelPropList = newModelType.GetProperties();
                var extModelPropList = extModelType.GetProperties();

                var modifiedPropList = newModelPropList.Join(extModelPropList, ex => ex.Name, nw => nw.Name, (nw, ex) => new { nw, ex })
                                        .Where(x => x.nw.Name.Substring(0, 3) != "ex_"
                                            && ((x.nw.GetValue(newModel) != null) ? x.nw.GetValue(newModel).ToString() : "") != ((x.ex.GetValue(existModel) != null) ? x.ex.GetValue(existModel).ToString() : ""))
                                        .Select(z => new LogAtndc
                                        {
                                            TblId = tableId,
                                            RecId = recId,
                                            EmpSysId = empSysId,
                                            AtndcDate = atndcDate,
                                            SftId = sftId,
                                            Ssn = ssn,
                                            Property = z.nw.Name,
                                            TrType = 1, //0=> Create, 1 => Update, 2 => Delete
                                            OldVal = (z.ex.GetValue(existModel) != null) ? z.ex.GetValue(existModel).ToString() : "",
                                            NewVal = (z.nw.GetValue(newModel) != null) ? z.nw.GetValue(newModel).ToString() : "",
                                            CrtBy = userId
                                        }); ;
                if (modifiedPropList.Count() > 0)
                {
                    _hrmContext.LogAtndc.AddRange(modifiedPropList);
                }
                return 1;
            }
            catch { throw; }
        }

        public int delete(int tableId, int recId, int userId)
        {
            throw new NotImplementedException();
        }

    }
}
