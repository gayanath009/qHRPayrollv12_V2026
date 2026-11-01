using qHRM.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace qHRM.Data
{
    public class DashboardContext
    {
        #region ==============Member Variables==============
        private readonly qHRMasterContext _hrmContext;
        #endregion

        #region ==============Constructors==============
        public DashboardContext(qHRMasterContext hrmContext)
        {
            _hrmContext = hrmContext;
        }
        #endregion

        #region ==============Employee Info==============
        public NltyEmpFullSmry GetNltyEmpFullSmryByEntId(int entId)
        {
            try
            {
                var totEmpCount = _hrmContext.HrEmpGenInfo.Where(z => z.CurrStat == 0).Count();
                NltyEmpFullSmry nltyEmpFullSmry = new NltyEmpFullSmry();
                nltyEmpFullSmry.AllocatedNltyEmpList = GetNltyWiseEmpRatio(entId);
                nltyEmpFullSmry.ActualNltyEmpList = GetNltyWiseEmpCount(totEmpCount, entId);
                nltyEmpFullSmry.NltyEmpCntSmryList = from alctd in nltyEmpFullSmry.AllocatedNltyEmpList
                                                     join actl in nltyEmpFullSmry.ActualNltyEmpList on alctd.EmpRtoId equals actl.EmpRtoId
                                                     select new NltyEmpCntSmry
                                                     {
                                                         EmpRtoId = alctd.EmpRtoId,
                                                         NltyId = alctd.NltyId,
                                                         NltyName = alctd.NltyName,
                                                         AlctdEmpCnt = (int)(totEmpCount * (alctd.StfRatio/100)),
                                                         ActlEmpCnt = (int)actl.EmpCount,
                                                         ActlInEmpCnt = (int)actl.InEmpCnt,
                                                         ActlOsEmpCnt = (int)actl.OsEmpCnt
                                                     };
                nltyEmpFullSmry.NltyEmpRatioList = nltyEmpFullSmry.ActualNltyEmpList.Join(nltyEmpFullSmry.AllocatedNltyEmpList, alc => alc.EmpRtoId, act => act.EmpRtoId, (act, alc) => new { act, alc })
                                                    .Select(x => new NltyEmpRatio
                                                    {
                                                        EmpRtoId = x.act.EmpRtoId,
                                                        NltyId = x.act.NltyId,
                                                        NltyName = x.act.NltyName,
                                                        ActStfRatio = x.act.StfRatio,
                                                        AlcStfRatio = x.alc.StfRatio
                                                    }).ToList(); ;
                return nltyEmpFullSmry;
            }
            catch { throw; }
        }

        private IEnumerable<NltyEmpCount> GetNltyWiseEmpRatio(int entId)
        {
            try
            {
                var result = _hrmContext.OrgNltyEmpRatio
                            .Join(_hrmContext.OrgNationality, nlty => nlty.NltyId, rto => rto.NltyId, (rto, nlty) => new { rto, nlty })
                            .Where(z => z.rto.NltyId != null && z.rto.EntId == entId)
                            .Select(x => new NltyEmpCount
                            {
                                EmpRtoId = x.rto.EmpRtoId,
                                NltyId = x.nlty.NltyId,
                                NltyName = x.nlty.NltyNameEng,
                                EmpCount = 0,
                                StfRatio = x.rto.StfRto,  
                            }).Concat(_hrmContext.OrgNltyEmpRatio
                                .Join(_hrmContext.OrgNltyType, nltyTyp => nltyTyp.NltyTypId, rto => rto.NltyTypId, (rto, nltyTyp) => new { rto, nltyTyp })
                                .Where(z => z.rto.NltyId == null)
                                .Select(x => new NltyEmpCount
                                {
                                    EmpRtoId = x.rto.EmpRtoId,
                                    NltyId = x.nltyTyp.NltyTypId,
                                    NltyName = x.nltyTyp.NltyTypNameEng,
                                    EmpCount = 0,
                                    StfRatio = x.rto.StfRto,
                                })
                            );
                return result;
            }
            catch (Exception ex) { throw ex; }
        }

        private IEnumerable<NltyEmpCount> GetNltyWiseEmpCount(int totEmpCount, int entId)
        {
            var result = (from rto in _hrmContext.OrgNltyEmpRatio
                                join nlty in _hrmContext.OrgNationality on rto.NltyId equals nlty.NltyId
                                join empCnt in (from emp in _hrmContext.HrEmpGenInfo
                                                    where emp.CurrStat == 0
                                                    group emp by emp.NltyId into g
                                                    select new NltyEmpCount { NltyId = g.Key, EmpCount = g.Count() }) 
                                    on nlty.NltyId equals empCnt.NltyId into nltyEmpCnts
                                from nltyEmpCnt in nltyEmpCnts.DefaultIfEmpty()
                                join osEmpCnt in (from emp in _hrmContext.HrEmpGenInfo
                                                    join loc in _hrmContext.OrgLocation on emp.LocId equals loc.LocId
                                                    where emp.CurrStat == 0 && loc.ParentId == 3
                                                    group emp by emp.NltyId into g
                                                    select new NltyEmpCount { NltyId = g.Key, OsEmpCnt = g.Count() })
                                    on nlty.NltyId equals osEmpCnt.NltyId into nltyOsEmpCnts
                                from nltyOsEmpCnt in nltyOsEmpCnts.DefaultIfEmpty()
                                where  rto.EntId == entId
                                select new NltyEmpCount
                                {
                                    EmpRtoId = rto.EmpRtoId,
                                    NltyId = nlty.NltyId,
                                    NltyName = nlty.NltyNameEng,
                                    EmpCount = (nltyEmpCnt.EmpCount == null) ? 0 : nltyEmpCnt.EmpCount,
                                    OsEmpCnt = (nltyOsEmpCnt.OsEmpCnt == null)? 0 : nltyOsEmpCnt.OsEmpCnt,
                                    StfRatio = (totEmpCount > 0) ? (nltyEmpCnt.EmpCount == null) ? 0 : Math.Round((((Convert.ToDecimal(nltyEmpCnt.EmpCount))/ totEmpCount) * 100),2) : 0
                                }).ToList();

            result.AddRange((from rto in _hrmContext.OrgNltyEmpRatio
                                join nltyTyp in _hrmContext.OrgNltyType on rto.NltyTypId equals nltyTyp.NltyTypId
                                join empCnt in (from nlty in _hrmContext.OrgNationality
                                                join emp in _hrmContext.HrEmpGenInfo on nlty.NltyId equals emp.NltyId
                                                where emp.CurrStat == 0
                                                group nlty by nlty.NltyTypId into g
                                                select new NltyEmpCount { NltyId = g.Key, EmpCount = g.Count() })
                                    on nltyTyp.NltyTypId equals empCnt.NltyId into nltyEmpCnts
                                from nltyEmpCnt in nltyEmpCnts.DefaultIfEmpty()
                                join OsEmpCnt in (from nlty in _hrmContext.OrgNationality
                                                join emp in _hrmContext.HrEmpGenInfo on nlty.NltyId equals emp.NltyId
                                                join loc in _hrmContext.OrgLocation on emp.LocId equals loc.LocId
                                                where emp.CurrStat == 0 && loc.ParentId == 3
                                                    group nlty by nlty.NltyTypId into g
                                                select new NltyEmpCount { NltyId = g.Key, OsEmpCnt = g.Count() })
                                    on nltyTyp.NltyTypId equals OsEmpCnt.NltyId into nltyOsEmpCnts
                                from nltyOsEmpCnt in nltyOsEmpCnts.DefaultIfEmpty()
                                where rto.NltyId == null && rto.EntId == entId
                          select new NltyEmpCount
                          {
                              EmpRtoId = rto.EmpRtoId,
                              NltyId = nltyTyp.NltyTypId,
                              NltyName = nltyTyp.NltyTypNameEng,
                              EmpCount = (nltyEmpCnt.EmpCount == null) ? 0 : nltyEmpCnt.EmpCount,
                              OsEmpCnt = (nltyOsEmpCnt.OsEmpCnt == null) ? 0 : nltyOsEmpCnt.OsEmpCnt,
                              StfRatio = (totEmpCount > 0) ? (nltyEmpCnt.EmpCount == null) ? 0 : Math.Round((((Convert.ToDecimal(nltyEmpCnt.EmpCount)) / totEmpCount) * 100), 2) : 0
                          }).ToList());
            return result;
        }

        public IEnumerable<DeptEmpSmry> GetDeptEmpSmry(int entId)
        {
            try
            {
                return from dpt in _hrmContext.OrgDepartment
                            join empCnt in (from jb in _hrmContext.HrEmpJobAssign
                                            join emp in _hrmContext.HrEmpGenInfo on jb.EmpSysId equals emp.EmpSysId
                                            where jb.WkStat == 1 && emp.CurrStat == 0
                                            group jb by jb.DeptId into g                                            
                                            select new DeptEmpSmry { DeptId = g.Key, EmpCnt = g.Count() })
                            on dpt.DeptId equals empCnt.DeptId
                            where dpt.RecStat == 1 && dpt.EntId == entId     
                            orderby empCnt.EmpCnt descending
                            select new DeptEmpSmry
                            {
                                DeptCode = dpt.DeptCode,
                                EmpCnt = empCnt.EmpCnt
                            };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region ==============Partial Classes==============
        public partial class NltyEmpCount
        {
            public int EmpRtoId { get; set; }
            public int NltyId { get; set; }
            public string NltyName { get; set; }
            public decimal StfRatio { get; set; }
            public int? EmpCount { get; set; }
            public int? OsEmpCnt { get; set; }
            public int? InEmpCnt { get { return (EmpCount - OsEmpCnt); } }
        }

        public partial class NltyEmpCntSmry
        {
            public int EmpRtoId { get; set; }
            public int NltyId { get; set; }
            public string NltyName { get; set; }
            public int AlctdEmpCnt { get; set; }
            public int ActlEmpCnt { get; set; }
            public int ActlInEmpCnt { get; set; }
            public int ActlOsEmpCnt { get; set; }
        }

        public partial class NltyEmpRatio
        {
            public int EmpRtoId { get; set; }
            public int NltyId { get; set; }
            public string NltyName { get; set; }
            public decimal ActStfRatio { get; set; }
            public decimal AlcStfRatio { get; set; }
        }

        public partial class NltyEmpFullSmry
        {
            public IEnumerable<NltyEmpCount> AllocatedNltyEmpList { get; set; }
            public IEnumerable<NltyEmpCount> ActualNltyEmpList { get; set; }
            public IEnumerable<NltyEmpCntSmry> NltyEmpCntSmryList { get; set; }
            public IEnumerable<NltyEmpRatio> NltyEmpRatioList { get; set; }
        }

        public partial class DeptEmpSmry
        {
            public int DeptId { get; set; }
            public string DeptCode { get; set; }
            public int EmpCnt { get; set; }
        }

        //Attendance
       
        #endregion Partial Classes
    }
}
