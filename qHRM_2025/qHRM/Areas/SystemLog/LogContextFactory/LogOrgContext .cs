using qHRM.Models;
using qHRM.Areas.SystemLog.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace qHRM.Areas.SystemLog.LogContextFactory
{
    public class LogOrgContext : ILogModelContext
    {
        private qHRMasterContext _hrmContext;
        public LogOrgContext(qHRMasterContext hrmContext)
        {
            _hrmContext = hrmContext;
        }
    
        public int insert(int tableId, int recId, int userId)
        {
            try
            {
                _hrmContext.LogOrg.Add(new LogOrg
                {
                    TblId = tableId,
                    RecId = recId,
                    Ssn = Convert.ToInt64(DateTime.Now.ToString("yyyyMMddhhmmssfff")),
                    Property = "New Record",
                    TrType = 0, //0=> Create, 1 => Update, 2 => Delete
                    OldVal = "",
                    NewVal = "",
                    CrtBy = userId
                });
                return 1;
            }
            catch { throw; }
        }

        public int update<T>(int tableId, int recId, T newModel, T existModel, int userId)
        {
            try
            {
                var newModelType = newModel.GetType();
                var extModelType = existModel.GetType();
                var ssn = Convert.ToInt64(DateTime.Now.ToString("yyyyMMddhhmmssfff"));

                var newModelPropList = newModelType.GetProperties();
                var extModelPropList = extModelType.GetProperties();


                var modifiedPropList = newModelPropList.Join(extModelPropList, ex => ex.Name, nw => nw.Name, (nw, ex) => new { nw, ex })
                                        .Where(x => x.nw.Name.Substring(0, 3) != "ex_" && ((x.nw.GetValue(newModel) != null) ? x.nw.GetValue(newModel).ToString() : "") != ((x.ex.GetValue(existModel) != null) ? x.ex.GetValue(existModel).ToString() : ""))
                                        .Select(z => new LogOrg
                                        {
                                            TblId = tableId,
                                            RecId = recId,
                                            Ssn = ssn,
                                            Property = z.nw.Name,
                                            TrType = 1, //0=> Create, 1 => Update, 2 => Delete
                                            OldVal = (z.ex.GetValue(existModel) != null) ? z.ex.GetValue(existModel).ToString() : "",
                                            NewVal = (z.nw.GetValue(newModel) != null) ? z.nw.GetValue(newModel).ToString() : "",
                                            CrtBy = userId
                                        });
                if (modifiedPropList.Count() > 0)
                {
                    _hrmContext.LogOrg.AddRange(modifiedPropList);
                }
                return 1;
            }
            catch{ throw; }
        }

        public int delete(int tableId, int recId, int userId)
        {
            try
            {
                _hrmContext.LogOrg.Add(new LogOrg
                {
                    TblId = tableId,
                    RecId = recId,
                    Ssn = Convert.ToInt64(DateTime.Now.ToString("yyyyMMddhhmmssfff")),
                    Property = "Record Deleted",
                    TrType = 2, //0=> Create, 1 => Update, 2 => Delete
                    OldVal = "",
                    NewVal = "",
                    CrtBy = userId
                });
                return 1;
            }
            catch { throw; }
        }
    }
}
