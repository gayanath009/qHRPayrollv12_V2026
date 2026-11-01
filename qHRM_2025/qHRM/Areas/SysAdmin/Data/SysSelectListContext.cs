using qHRM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace qHRM.Areas.SysAdmin.Data
{
    public class SysSelectListContext
    {
        private readonly qHRMasterContext _hrmContext;

        public SysSelectListContext(qHRMasterContext hrmContext)
        {
            this._hrmContext = hrmContext;
        }

        public partial class ListElement
        {
            public short Indx { get; set; }
            public string ElmntTxt { get; set; }
            public bool? ResTyp { get; set; }
            public int LstElmntId { get; set; }
        }

        public partial class ListElementWithCustIndex
        {
            public int CustIndx { get; set; }
            public short Indx { get; set; }
            public string ElmntTxt { get; set; }
            public bool? ResTyp { get; set; }
            public int LstElmntId { get; set; }
        }

        public IEnumerable<ListElement> GetElements(SelectListTypeEnum typKey)
        {
            try
            {
                return _hrmContext.SysSelectListElement
                        .Join(_hrmContext.SysSelectListType, elm => elm.LstTypId, typ => typ.LstTypId, (elm, typ) => new { elm, typ })
                        .Where(z => z.typ.LstTypKey == typKey.ToString())
                        .Select(x => new ListElement
                        {
                            Indx = x.elm.Indx
                             , ElmntTxt = x.elm.ElmntTxt
                             , ResTyp = x.elm.ResTyp
                             , LstElmntId = x.elm.LstElmntId
                        }).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<ListElementWithCustIndex> GetMonths(SelectListTypeEnum typKey, int stMonth, int count)
        {
            try
            {
                var temp = GetElements(SelectListTypeEnum.MONTHS);

                var monthlist = new List<ListElementWithCustIndex>();

                int i = 0;
                foreach (int n in Enumerable.Range(stMonth, count).ToList())
                {
                    var elmnt = temp.Where(x => x.Indx == (Int16)((n > 12) ? n - 12 : n)).FirstOrDefault();
                    monthlist.Add(new ListElementWithCustIndex
                                    {
                        
                                        CustIndx = i + 1,
                                        Indx = elmnt.Indx,
                                        ElmntTxt = elmnt.ElmntTxt,
                                        ResTyp = elmnt.ResTyp,
                                        LstElmntId = elmnt.LstElmntId
                                    });
                }
            return monthlist;
            }
            catch (Exception ex) { throw ex; }
        }

        public enum SelectListTypeEnum
        {
            REC_STAT        //Record Status
            , TITL          //Title
            , GND           //Gender
            , EMP_TYP       //Employee Type
            , PMT_TYP       //Permit Type
            , EMP_DOC_TYP   //Employee Document Type
            , DPN_DOC_TYP   //Dependent Document Type
            , MRTL_STAT     //Marital Status
            , BLD_GRP       //Blood Group
            , EMP_CUR_STAT  //Employee Current Status
            , BNK_ACCT_TYP  //Bank Account Type
            , DPN_OCC_TYP   //Dependent Occupation Type
            , EMP_RSD_TYP   //Residential Type
            , SPNSR_TYP     //Sponsor Type
            , RLTN_TYP      //Relationship Type
            , TCKT_ALGB     //Air Ticket Eligibility
            , MONTHS        //Months
            , INCTV_MOD     //Incentive Mode
            , INCTV_TYP     //Incentive Type  
            , DAYS          //Days
            , LV_RJN_STS	//Rejoin Status
        }
    }
}
