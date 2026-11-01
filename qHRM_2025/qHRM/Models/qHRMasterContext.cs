using Microsoft.EntityFrameworkCore;
using qHRM.Areas.Employee.Models;
using qHRM.Areas.Entity.Models;
using qHRM.Areas.General.Models;
using qHRM.Areas.Leave.Models;
using qHRM.Areas.Payroll.Models;
using qHRM.Areas.SysAdmin.Models;
using qHRM.Areas.WorkFlow.Models;
using qHRM.Areas.WorkShift.Models;
using qHRM.Areas.Email.Models;
using qHRM.Areas.SystemLog.Models;
using qHRM.Areas.Attendance.Models;
using qHRM.Areas.Integrations.Models;
using qHRM.Areas.Travel;
using static qHRM.Areas.Payroll.Data.PayrollContext;
using static qHRM.Areas.Attendance.Data.AttnContext;
using qHRM.Areas.Reports.Models;

namespace qHRM.Models
{
    public partial class qHRMasterContext : DbContext
    {
        public qHRMasterContext()
        {
        }

        public qHRMasterContext(DbContextOptions<qHRMasterContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AdmEntityUser> AdmEntityUser { get; set; }
        public virtual DbSet<AdmUser> AdmUser { get; set; }
        public virtual DbSet<AdmUserView> AdmUserView { get; set; }
        public virtual DbSet<AdmUserGroup> AdmUserGroup { get; set; }
        public virtual DbSet<AdmUserGroupPrivilege> AdmUserGroupPrivilege { get; set; }
        public virtual DbSet<AdmUserLogs> AdmUserLogs { get; set; }
        public virtual DbSet<AdmUsrGrpEmpGrp> AdmUsrGrpEmpGrp { get; set; }
        public virtual DbSet<HrEmpAtndcSft> HrEmpAtndcSft { get; set; }
        public virtual DbSet<HrEmpAttendExp> HrEmpAttendExp { get; set; }
        public virtual DbSet<HrOvrtmNoPayView> HrOvrtmNoPayView { get; set; }
        public virtual DbSet<HrEmpAttnSummary> HrEmpAttnSummary { get; set; }
        public virtual DbSet<HrEmpAttendToday> HrEmpAttendToday { get; set; }
        public virtual DbSet<HrEmpBankAcct> HrEmpBankAcct { get; set; }
        public virtual DbSet<HrEmpCnfNtfLvl> HrEmpCnfNtfLvl { get; set; }
        public virtual DbSet<HrEmpConfig> HrEmpConfig { get; set; }
        public virtual DbSet<HrEmpDependent> HrEmpDependent { get; set; }
        public virtual DbSet<HrEmpDependentView> HrEmpDependentView { get; set; }
        public virtual DbSet<HrEmpDocument> HrEmpDocument { get; set; }
        public virtual DbSet<HrEmpDpndDoc> HrEmpDpndDoc { get; set; }
        public virtual DbSet<HrEmpEduQualif> HrEmpEduQualif { get; set; }
        public virtual DbSet<HrEmpGenInfo> HrEmpGenInfo { get; set; }
        public virtual DbSet<HrEmpGenInfoView> HrEmpGenInfoView { get; set; }
        public virtual DbSet<EmpView> EmpView { get; set; }
        public virtual DbSet<HrEmpGrade> HrEmpGrade { get; set; }
        public virtual DbSet<HrEmpGroup> HrEmpGroup { get; set; }
        public virtual DbSet<HrEmpJobAssign> HrEmpJobAssign { get; set; }
        public virtual DbSet<HrEmpPhoto> HrEmpPhoto { get; set; }
        public virtual DbSet<HrEmpPreExp> HrEmpPreExp { get; set; }
        public virtual DbSet<HrEmpProbation> HrEmpProbation { get; set; }
        public virtual DbSet<HrEmpResidInfo> HrEmpResidInfo { get; set; }
        public virtual DbSet<HrEmpSponsor> HrEmpSponsor { get; set; }
        public virtual DbSet<HrEmpWkRoster> HrEmpWkRoster { get; set; }
        public virtual DbSet<HrEmpWkShift> HrEmpWkShift { get; set; }
        public virtual DbSet<HrLvAnlSchedule> HrLvAnlSchedule { get; set; }
        public virtual DbSet<HrLvRegDocs> HrLvRegDocs { get; set; }
        public virtual DbSet<HrLvRegister> HrLvRegister { get; set; }
        public virtual DbSet<HrLvType> HrLvType { get; set; }
        public virtual DbSet<HrLvPolicy> HrLvPolicy { get; set; }

        // ── Leave Workflow & Balance Management Integration ───────────────────
        public virtual DbSet<HrLvWfConfig> HrLvWfConfig { get; set; }
        public virtual DbSet<HrLvWfInstance> HrLvWfInstance { get; set; }
        public virtual DbSet<HrLvWfDelegation> HrLvWfDelegation { get; set; }
        public virtual DbSet<HrLvWfNotifyLog> HrLvWfNotifyLog { get; set; }
        public virtual DbSet<HrWfUserMapping> HrWfUserMapping { get; set; }
        public virtual DbSet<HrLvOpeningBalance> HrLvOpeningBalance { get; set; }
        public virtual DbSet<HrLvRunningBalance> HrLvRunningBalance { get; set; }
        public virtual DbSet<HrLvBalanceTransaction> HrLvBalanceTransaction { get; set; }
        // ─────────────────────────────────────────────────────────────────────
        public virtual DbSet<HrPrAdHocTrns> HrPrAdHocTrns { get; set; }
        public virtual DbSet<HrPrAdHocTrnType> HrPrAdHocTrnType { get; set; }
        public virtual DbSet<HrPrCnfGrtvtyLvl> HrPrCnfGrtvtyLvl { get; set; }
        public virtual DbSet<HrPrConfig> HrPrConfig { get; set; }
        public virtual DbSet<HrPrEmpSlryStruc> HrPrEmpSlryStruc { get; set; }
        public virtual DbSet<HrPrEmpSlryStrucVw> HrPrEmpSlryStrucVw { get; set; }
        public virtual DbSet<HrPrEmpSlryStrucHstryVw> HrPrEmpSlryStrucHstryVw { get; set; }
        public virtual DbSet<HrPrInctv> HrPrInctv { get; set; }
        public virtual DbSet<HrPrLvEncsh> HrPrLvEncsh { get; set; }
        public virtual DbSet<HrPrOtrate> HrPrOtrate { get; set; }
        public virtual DbSet<HrPrProjAlwnc> HrPrProjAlwnc { get; set; }
        public virtual DbSet<HrPrSlryAtnAdj> HrPrSlryAtnAdj { get; set; }
        public virtual DbSet<HrPrSlryPrcDtlActvVw> HrPrSlryPrcDtlActv { get; set; }
        public virtual DbSet<HrPrSlryPrcDtlPsv> HrPrSlryPrcDtlPsv { get; set; }
        public virtual DbSet<HrPrSlryPrcExcld> HrPrSlryPrcExcld { get; set; }
        public virtual DbSet<HrPrSlryPrcHdr> HrPrSlryPrcHdr { get; set; }
        public virtual DbSet<HrPrSlryPrcLog> HrPrSlryPrcLog { get; set; }
        public virtual DbSet<HrPrSlryPrcLogDtl> HrPrSlryPrcLogDtl { get; set; }
        public virtual DbSet<HrPrStatutoryCmplnc> HrPrStatutoryCmplnc { get; set; }
        public virtual DbSet<HrPrStfLoan> HrPrStfLoan { get; set; }
        public virtual DbSet<HrPrStfLoanPmt> HrPrStfLoanPmt { get; set; }
        public virtual DbSet<HrPrStfLoanPmtExp> HrPrStfLoanPmtExp { get; set; }
        public virtual DbSet<HrPrStfLoanType> HrPrStfLoanType { get; set; }
        public virtual DbSet<HrTrvlTktDtl> HrTrvlTktDtl { get; set; }
        public virtual DbSet<HrWkRoster> HrWkRoster { get; set; }
        public virtual DbSet<HrWkRosterShift> HrWkRosterShift { get; set; }
        public virtual DbSet<HrWkShift> HrWkShift { get; set; }
        public virtual DbSet<LogAtndc> LogAtndc { get; set; }
        public virtual DbSet<LogAtndcLdPrc> LogAtndcLdPrc { get; set; }
        public virtual DbSet<LogHremp> LogHremp { get; set; }
        public virtual DbSet<LogHrlv> LogHrlv { get; set; }
        public virtual DbSet<LogHrpr> LogHrpr { get; set; }
        public virtual DbSet<LogHrwsf> LogHrwsf { get; set; }
        public virtual DbSet<LogLvLdPrc> LogLvLdPrc { get; set; }
        public virtual DbSet<LogOrg> LogOrg { get; set; }
        public virtual DbSet<OrgCountry> OrgCountry { get; set; }
        public virtual DbSet<OrgDepartment> OrgDepartment { get; set; }
        public virtual DbSet<OrgDesignation> OrgDesignation { get; set; }
        public virtual DbSet<OrgEmpDocType> OrgEmpDocType { get; set; }
        public virtual DbSet<OrgEmpCht> OrgEmpChtObj { get; set; }
        public virtual DbSet<OrgEntity> OrgEntity { get; set; }
        public virtual DbSet<OrgHoliday> OrgHoliday { get; set; }
        public virtual DbSet<OrgInfo> OrgInfo { get; set; }
        public virtual DbSet<OrgLocation> OrgLocation { get; set; }
        public virtual DbSet<OrgNationality> OrgNationality { get; set; }
        public virtual DbSet<OrgReligion> OrgReligion { get; set; }
        public virtual DbSet<OrgNltyEmpRatio> OrgNltyEmpRatio { get; set; }
        public virtual DbSet<OrgNltyType> OrgNltyType { get; set; }
        public virtual DbSet<OrgWrkType> OrgWrkType { get; set; }
        public virtual DbSet<ReCnfgRpt> ReCnfgRpts { get; set; }
        public virtual DbSet<ReCnfgRptPrm> ReCnfgRptPrms { get; set; }
        public virtual DbSet<ReRptLog> ReRptLogs { get; set; }
        public virtual DbSet<ReStgngRpt> ReStgngRpts { get; set; }
        public virtual DbSet<ReStgngRptPrm> ReStgngRptPrms { get; set; }
        public virtual DbSet<ReStgngSsn> ReStgngSsns { get; set; }
        public virtual DbSet<SysAtndcStat> SysAtndcStat { get; set; }
        public virtual DbSet<SysDshbrd> SysDshbrd { get; set; }
        public virtual DbSet<SysError> SysError { get; set; }
        public virtual DbSet<SysSelectListElement> SysSelectListElement { get; set; }
        public virtual DbSet<SysSelectListType> SysSelectListType { get; set; }
        public virtual DbSet<SysSystemPrivilege> SysSystemPrivilege { get; set; }
        public virtual DbSet<SysTableInfo> SysTableInfo { get; set; }
        public virtual DbSet<TmpLvTypMpng> TmpLvTypMpng { get; set; }
        public virtual DbSet<WfPolicyType> WfPolicyType { get; set; }
        public virtual DbSet<WfStage> WfStage { get; set; }
        public virtual DbSet<WfStageStatus> WfStageStatus { get; set; }
        public virtual DbSet<WfStatus> WfStatus { get; set; }
        public virtual DbSet<WfStrucAlert> WfStrucAlert { get; set; }
        public virtual DbSet<WfStrucStage> WfStrucStage { get; set; }
        public virtual DbSet<WfStrucStageAlert> WfStrucStageAlert { get; set; }
        public virtual DbSet<WfStructure> WfStructure { get; set; }

        //START ------- Custom Models ------- START//
        public virtual DbSet<mvGroupPrivilege> mvGroupPrivilege { get; set; }
        public virtual DbSet<SysConfig> SysConfig { get; set; }
        public virtual DbSet<Login> Login { get; set; }
        public virtual DbSet<HrPrSlryPrcDtlPsvVw> HrPrSlryPrcDtlPsvVw { get; set; }
        public virtual DbSet<HrEmpAtndcVw> HrEmpAtndcVws { get; set; }
        public virtual DbSet<HrEmpAtndcSftVw> HrEmpAtndcSftVw { get; set; }
        public virtual DbSet<HrEmpAbsntExpVw> HrEmpAbsntExpVw { get; set; }
        public virtual DbSet<HrEmpAtndcExpSmryVw> HrEmpAtndcExpSmryVw { get; set; }
        public virtual DbSet<LstMntSlryVw> LstMntSlryVw { get; set; }
        public virtual DbSet<LvSlryClcRsltVw> LvSlryClcRsltVw { get; set; }
        public virtual DbSet<HrEmpAtndcStngsSts> HrEmpAttnStngsStsVw { get; set; }
        public virtual DbSet<HrPrEmpSlryStngsSts> HrPrEmpSlryStngsStsVw { get; set; }


        public virtual DbSet<HrPrSlrySlipSndMailVw> HrPrSlrySlipSndMailVw { get; set; }

        public virtual DbSet<SbrdntEmpSysId> SbrdntEmpSysId { get; set; }        
        // END ------- Custom Models ------- END//

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlServer("Server=KELUM-NB1\\MSSQLSERVER2016;Database=qHRMaster_Vx10;uid=sa;password=sa#2016; Trusted_Connection=False;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AdmEntityUser>(entity =>
            {
                entity.HasKey(e => e.AutoId);

                entity.ToTable("ADM_Entity_User");

                entity.Property(e => e.AutoId).HasColumnName("Auto_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.UsrId).HasColumnName("Usr_Id");

                entity.HasOne(d => d.Ent)
                    .WithMany(p => p.AdmEntityUser)
                    .HasForeignKey(d => d.EntId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ADM_Entity_User_ORG_Entity");

                entity.HasOne(d => d.Usr)
                    .WithMany(p => p.AdmEntityUser)
                    .HasForeignKey(d => d.UsrId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ADM_Entity_User_ADM_User");
            });

            modelBuilder.Entity<AdmUser>(entity =>
            {
                entity.HasKey(e => e.UsrId);

                entity.ToTable("ADM_User");

                entity.Property(e => e.UsrId).HasColumnName("Usr_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.GrpId).HasColumnName("Grp_Id");

                entity.Property(e => e.Password)
                    .HasColumnName("Password")
                    .HasMaxLength(255);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.Remarks).HasMaxLength(200);

                entity.Property(e => e.UsrName)
                    .IsRequired()
                    .HasColumnName("Usr_Name")
                    .HasMaxLength(50);

                entity.HasOne(d => d.Grp)
                    .WithMany(p => p.AdmUser)
                    .HasForeignKey(d => d.GrpId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ADM_User_ADM_UserGroup");
            });

            modelBuilder.Entity<AdmUserGroup>(entity =>
            {
                entity.HasKey(e => e.GrpId)
                    .HasName("PK_UserGroup");

                entity.ToTable("ADM_UserGroup");

                entity.Property(e => e.GrpId).HasColumnName("Grp_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Desc).HasMaxLength(100);

                entity.Property(e => e.DshbrdId).HasColumnName("Dshbrd_Id");

                entity.Property(e => e.EmpGrpSbs).HasColumnName("Emp_Grp_Sbs");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.ShwAlEmp).HasColumnName("Shw_Al_Emp");

                entity.HasOne(d => d.Dshbrd)
                    .WithMany(p => p.AdmUserGroup)
                    .HasForeignKey(d => d.DshbrdId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ADM_UserGroup_SYS_Dshbrd");
            });

            modelBuilder.Entity<AdmUsrGrpEmpGrp>(entity =>
            {
                entity.HasKey(e => e.AutoId);

                entity.ToTable("ADM_UserGroupEmpGroup");

                entity.Property(e => e.AutoId).HasColumnName("Auto_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.EmpGrpId).HasColumnName("EmpGrp_Id");

                entity.Property(e => e.GrpId).HasColumnName("Grp_Id");

                entity.Property(e => e.RecAdd).HasColumnName("Rec_Add");

                entity.Property(e => e.RecDelete).HasColumnName("Rec_Delete");

                entity.Property(e => e.RecEdit).HasColumnName("Rec_Edit");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RecView).HasColumnName("Rec_View");

                entity.HasOne(d => d.EmpGrp)
                    .WithMany(p => p.AdmUsrGrpEmpGrp)
                    .HasForeignKey(d => d.EmpGrpId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ADM_UserGroupEmpGroup_HR_EmpGroup");

                entity.HasOne(d => d.Grp)
                    .WithMany(p => p.AdmUsrGrpEmpGrp)
                    .HasForeignKey(d => d.GrpId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ADM_UserGroupEmpGroup_ADM_UserGroup");
            });

            modelBuilder.Entity<AdmUserGroupPrivilege>(entity =>
            {
                entity.HasKey(e => e.AutoId);

                entity.ToTable("ADM_UserGroupPrivilege");

                entity.Property(e => e.AutoId).HasColumnName("Auto_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.GrpId).HasColumnName("Grp_Id");

                entity.Property(e => e.PrvId).HasColumnName("Prv_Id");

                entity.Property(e => e.RecAdd).HasColumnName("Rec_Add");

                entity.Property(e => e.RecDelete).HasColumnName("Rec_Delete");

                entity.Property(e => e.RecEdit).HasColumnName("Rec_Edit");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RecView).HasColumnName("Rec_View");

                entity.HasOne(d => d.Grp)
                    .WithMany(p => p.AdmUserGroupPrivilege)
                    .HasForeignKey(d => d.GrpId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ADM_UserGroupPrivilege_ADM_UserGroup");

                entity.HasOne(d => d.Prv)
                    .WithMany(p => p.AdmUserGroupPrivilege)
                    .HasForeignKey(d => d.PrvId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ADM_UserGroupPrivilege_SYS_SystemPrivilege");
            });

            modelBuilder.Entity<AdmUserLogs>(entity =>
            {
                entity.HasKey(e => e.LogId);

                entity.ToTable("ADM_UserLogs");

                entity.Property(e => e.SessionId).HasColumnName("Session_Id");

                entity.Property(e => e.UsrId)
                    .HasColumnName("Usr_Id")
                    .ValueGeneratedNever();

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.InTime)
                    .HasColumnName("In_Time")
                    .HasColumnType("datetime");

                entity.Property(e => e.LogId)
                    .HasColumnName("Log_Id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.OutTime)
                    .HasColumnName("Out_Time")
                    .HasColumnType("datetime");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.HasOne(d => d.Usr)
                    .WithOne(p => p.AdmUserLogs)
                    .HasForeignKey<AdmUserLogs>(d => d.UsrId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ADM_UserLogs_ADM_User");
            });

            modelBuilder.Entity<HrEmpAtndcSft>(entity =>
            {
                entity.HasKey(e => e.AtndcSftId);

                entity.ToTable("HR_EmpAtndcSft");

                entity.Property(e => e.AtndcSftId).HasColumnName("Atndc_Sft_Id");

                entity.Property(e => e.AtndcDate)
                    .HasColumnName("Atndc_Date")
                    .HasColumnType("date");

                entity.Property(e => e.AtndcStatId).HasColumnName("Atndc_Stat_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.ErlyLvMnt).HasColumnName("Erly_Lv_Mnt");

                entity.Property(e => e.ExtOtMnt).HasColumnName("ExtOT_Mnt");

                entity.Property(e => e.InSource)
                    .HasColumnName("In_Source")
                    .HasMaxLength(10);

                entity.Property(e => e.InTime).HasColumnName("In_Time");

                entity.Property(e => e.LateInMnt).HasColumnName("Late_In_Mnt");

                entity.Property(e => e.MnlMdfd).HasColumnName("Mnl_Mdfd");

                entity.Property(e => e.Note).HasMaxLength(100);

                entity.Property(e => e.OtMnt).HasColumnName("OT_Mnt");

                entity.Property(e => e.OutSource)
                    .HasColumnName("Out_Source")
                    .HasMaxLength(10);

                entity.Property(e => e.OutTime).HasColumnName("Out_Time");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.SftId).HasColumnName("Sft_Id");

                entity.Property(e => e.SlryDctFct)
                    .HasColumnName("Slry_Dct_Fct")
                    .HasColumnType("decimal(3, 2)");

                entity.Property(e => e.WrkMnt).HasColumnName("Wrk_Mnt");

                entity.HasOne(d => d.AtndcStat)
                    .WithMany(p => p.HrEmpAtndcSft)
                    .HasForeignKey(d => d.AtndcStatId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpAtndcSft_SYS_AtndcStat");

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrEmpAtndcSft)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpAtndcSft_HR_EmpGenInfo");

                entity.HasOne(d => d.Sft)
                    .WithMany(p => p.HrEmpAtndcSft)
                    .HasForeignKey(d => d.SftId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpAtndcSft_HR_WkShift");
            });

            modelBuilder.Entity<HrEmpAttendExp>(entity =>
            {
                entity.HasKey(e => e.AtnExpId);

                entity.ToTable("HR_EmpAttendExp");

                entity.Property(e => e.AtnExpId).HasColumnName("AtnExp_Id");

                entity.Property(e => e.AprvdDateTime)
                    .HasColumnName("Aprvd_DateTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.AprvdId).HasColumnName("Aprvd_Id");

                entity.Property(e => e.AprvdRmks)
                    .HasColumnName("Aprvd_Rmks")
                    .HasMaxLength(200);

                entity.Property(e => e.AprvdStatId)
                    .HasColumnName("Aprvd_Stat_Id")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.EndDate)
                    .HasColumnName("End_Date")
                    .HasColumnType("date");

                entity.Property(e => e.Note).HasMaxLength(300);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RqstId).HasColumnName("Rqst_Id");

                entity.Property(e => e.StDate)
                    .HasColumnName("St_Date")
                    .HasColumnType("date");

                entity.HasOne(d => d.Aprvd)
                    .WithMany(p => p.HrEmpAttendExpAprvd)
                    .HasForeignKey(d => d.AprvdId)
                    .HasConstraintName("FK_HR_EmpAttendExp_HR_EmpGenInfo_Aprvd");

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrEmpAttendExpEmpSys)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpAttendExp_HR_EmpGenInfo");

                entity.HasOne(d => d.Rqst)
                    .WithMany(p => p.HrEmpAttendExpRqst)
                    .HasForeignKey(d => d.RqstId)
                    .HasConstraintName("FK_HR_EmpAttendExp_HR_EmpGenInfo_Rqst");
            });

            modelBuilder.Entity<HrEmpBankAcct>(entity =>
            {
                entity.HasKey(e => e.AcctId);

                entity.ToTable("HR_EmpBankAcct");

                entity.HasIndex(e => e.AcctNo)
                    .IsUnique();

                entity.Property(e => e.AcctId).HasColumnName("Acct_Id");

                entity.Property(e => e.AcctName)
                    .HasColumnName("Acct_Name")
                    .HasMaxLength(100);

                entity.Property(e => e.AcctNo)
                    .IsRequired()
                    .HasColumnName("Acct_No")
                    .HasMaxLength(35);

                entity.Property(e => e.AcctType)
                    .IsRequired()
                    .HasColumnName("Acct_Type")
                    .HasMaxLength(20);

                entity.Property(e => e.BankName)
                    .IsRequired()
                    .HasColumnName("Bank_Name")
                    .HasMaxLength(50);

                entity.Property(e => e.BranchName)
                    .HasColumnName("Branch_Name")
                    .HasMaxLength(20);

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.Iban).HasMaxLength(30);

                entity.Property(e => e.OpndDate)
                    .HasColumnName("Opnd_Date")
                    .HasColumnType("date");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.ShtBankName)
                    .HasColumnName("Sht_Bank_Name")
                    .HasMaxLength(10);

                entity.Property(e => e.SlryAcct).HasColumnName("Slry_Acct");

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrEmpBankAcct)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpBankAcct_HR_EmpGenInfo");
            });

            modelBuilder.Entity<HrEmpCnfNtfLvl>(entity =>
            {
                entity.HasKey(e => e.NtfLvlId);

                entity.ToTable("HR_EmpCnfNtfLvl");

                entity.Property(e => e.NtfLvlId).HasColumnName("Ntf_Lvl_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.Desc).HasMaxLength(100);

                entity.Property(e => e.EftDays).HasColumnName("Eft_Days");

                entity.Property(e => e.EmpCnfId).HasColumnName("Emp_Cnf_Id");

                entity.Property(e => e.LwrBndryYrs).HasColumnName("Lwr_Bndry_Yrs");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.UprBndryYrs).HasColumnName("Upr_Bndry_Yrs");

                entity.HasOne(d => d.EmpCnf)
                    .WithMany(p => p.HrEmpCnfNtfLvl)
                    .HasForeignKey(d => d.EmpCnfId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpCnfNtfLvl_HR_EmpConfig");
            });

            modelBuilder.Entity<HrEmpConfig>(entity =>
            {
                entity.HasKey(e => e.EmpCnfId);

                entity.ToTable("HR_EmpConfig");

                entity.Property(e => e.EmpCnfId).HasColumnName("Emp_Cnf_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.DftPrbNtf).HasColumnName("Dft_Prb_Ntf");

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.Notes).HasMaxLength(200);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.HasOne(d => d.Ent)
                    .WithMany(p => p.HrEmpConfig)
                    .HasForeignKey(d => d.EntId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpConfig_ORG_Entity");
            });

            modelBuilder.Entity<HrEmpDependent>(entity =>
            {
                entity.HasKey(e => e.DpndId);

                entity.ToTable("HR_EmpDependent");

                entity.Property(e => e.DpndId).HasColumnName("Dpnd_Id");

                entity.Property(e => e.BldGrp)
                    .HasColumnName("Bld_Grp")
                    .HasMaxLength(4);

                entity.Property(e => e.CntryId).HasColumnName("Cntry_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Dob)
                    .HasColumnName("DOB")
                    .HasColumnType("date");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasColumnName("First_Name")
                    .HasMaxLength(30);

                entity.Property(e => e.Gender)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.Institute).HasMaxLength(30);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasColumnName("Last_Name")
                    .HasMaxLength(30);

                entity.Property(e => e.MblPhone)
                    .HasColumnName("Mbl_Phone")
                    .HasMaxLength(20);

                entity.Property(e => e.MdlName)
                    .HasColumnName("Mdl_Name")
                    .HasMaxLength(30);

                entity.Property(e => e.NameArb)
                    .IsRequired()
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(150);

                entity.Property(e => e.NltyId).HasColumnName("Nlty_Id");

                entity.Property(e => e.Note).HasMaxLength(300);

                entity.Property(e => e.Occupation)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.PhotoMime)
                    .HasColumnName("Photo_Mime")
                    .HasMaxLength(20);

                entity.Property(e => e.PhotoName)
                    .HasColumnName("Photo_Name")
                    .HasMaxLength(50);

                entity.Property(e => e.PmtExpDate)
                    .HasColumnName("Pmt_Exp_Date")
                    .HasColumnType("date");

                entity.Property(e => e.PmtId)
                    .IsRequired()
                    .HasColumnName("Pmt_Id")
                    .HasMaxLength(20);

                entity.Property(e => e.PmtTyp)
                    .IsRequired()
                    .HasColumnName("Pmt_Typ")
                    .HasMaxLength(20);

                entity.Property(e => e.PsnEmail)
                    .HasColumnName("Psn_Email")
                    .HasMaxLength(50);

                entity.Property(e => e.PstCntryId).HasColumnName("Pst_Cntry_Id");

                entity.Property(e => e.PstExpDate)
                    .HasColumnName("Pst_Exp_Date")
                    .HasColumnType("date");

                entity.Property(e => e.PstName)
                    .IsRequired()
                    .HasColumnName("Pst_Name")
                    .HasMaxLength(150);

                entity.Property(e => e.PstNo)
                    .HasColumnName("Pst_No")
                    .HasMaxLength(15);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RltnTyp)
                    .IsRequired()
                    .HasColumnName("Rltn_Typ")
                    .HasMaxLength(20);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.HasOne(d => d.Cntry)
                    .WithMany(p => p.HrEmpDependent)
                    .HasForeignKey(d => d.CntryId)
                    .HasConstraintName("FK_HR_EmpDependent_ORG_Country");

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrEmpDependent)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpDependent_HR_EmpGenInfo");

                entity.HasOne(d => d.Nlty)
                    .WithMany(p => p.HrEmpDependent)
                    .HasForeignKey(d => d.NltyId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpDependent_ORG_Nationality");
            });

            modelBuilder.Entity<HrEmpDocument>(entity =>
            {
                entity.HasKey(e => e.DocId);

                entity.ToTable("HR_EmpDocument");

                entity.Property(e => e.DocId).HasColumnName("Doc_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.Desc)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.DocMime)
                    .HasColumnName("Doc_Mime")
                    .HasMaxLength(100);

                entity.Property(e => e.DocName)
                    .HasColumnName("Doc_Name")
                    .HasMaxLength(100);

                entity.Property(e => e.DocTypId).HasColumnName("Doc_Typ_Id");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.ScnCopy).HasColumnName("Scn_Copy");

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrEmpDocument)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpDocument_HR_EmpGenInfo");
            });

            modelBuilder.Entity<HrEmpDpndDoc>(entity =>
            {
                entity.HasKey(e => e.DocId);

                entity.ToTable("HR_EmpDpndDoc");

                entity.Property(e => e.DocId).HasColumnName("Doc_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.Desc)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.DocMime)
                    .HasColumnName("Doc_Mime")
                    .HasMaxLength(100);

                entity.Property(e => e.DocName)
                    .HasColumnName("Doc_Name")
                    .HasMaxLength(100);

                entity.Property(e => e.DocTypId).HasColumnName("Doc_Typ_Id");

                entity.Property(e => e.DpndId).HasColumnName("Dpnd_Id");

                entity.Property(e => e.ScnCopy).HasColumnName("Scn_Copy");

                entity.HasOne(d => d.Dpnd)
                    .WithMany(p => p.HrEmpDpndDoc)
                    .HasForeignKey(d => d.DpndId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpDpndDoc_HR_EmpDependent");
            });

            modelBuilder.Entity<HrEmpEduQualif>(entity =>
            {
                entity.HasKey(e => e.QualifId);

                entity.ToTable("HR_EmpEduQualif");

                entity.HasIndex(e => e.CertNo)
                    .IsUnique();

                entity.Property(e => e.QualifId).HasColumnName("Qualif_Id");

                entity.Property(e => e.CertMime)
                    .HasColumnName("Cert_Mime")
                    .HasMaxLength(20);

                entity.Property(e => e.CertName)
                    .HasColumnName("Cert_Name")
                    .HasMaxLength(50);

                entity.Property(e => e.CertNo)
                    .IsRequired()
                    .HasColumnName("Cert_No")
                    .HasMaxLength(30);

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Desc)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.EndDate)
                    .HasColumnName("End_Date")
                    .HasColumnType("date");

                entity.Property(e => e.Institute)
                    .IsRequired()
                    .HasMaxLength(300);

                entity.Property(e => e.Result)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.StDate)
                    .HasColumnName("St_Date")
                    .HasColumnType("date");

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrEmpEduQualif)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpEduQualif_HR_EmpGenInfo");
            });

            modelBuilder.Entity<HrEmpGenInfo>(entity =>
            {
                entity.HasKey(e => e.EmpSysId);

                entity.ToTable("HR_EmpGenInfo");

                entity.HasIndex(e => e.EmpId)
                    .HasName("IX_HR_HR_EmpGenInfo_Id")
                    .IsUnique();

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.AdId)
                    .IsRequired()
                    .HasColumnName("Ad_Id")
                    .HasMaxLength(20);

                entity.Property(e => e.AddrCntryId).HasColumnName("Addr_Cntry_Id");

                entity.Property(e => e.BldGrp)
                    .HasColumnName("Bld_Grp")
                    .HasMaxLength(4);

                entity.Property(e => e.BldNo)
                    .HasColumnName("Bld_No")
                    .HasMaxLength(10);

                entity.Property(e => e.City).HasMaxLength(30);

                entity.Property(e => e.CntryId).HasColumnName("Cntry_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CurrStat).HasColumnName("Curr_Stat");

                entity.Property(e => e.Dob)
                    .HasColumnName("DOB")
                    .HasColumnType("date");

                entity.Property(e => e.DrvLcnExpDate)
                    .HasColumnName("Drv_Lcn_Exp_Date")
                    .HasColumnType("date");

                entity.Property(e => e.DrvLcnNo)
                    .HasColumnName("Drv_Lcn_No")
                    .HasMaxLength(15);

                entity.Property(e => e.EmgCnt1)
                    .IsRequired()
                    .HasColumnName("Emg_Cnt_1")
                    .HasMaxLength(15);

                entity.Property(e => e.EmgCnt2)
                    .HasColumnName("Emg_Cnt_2")
                    .HasMaxLength(15);

                entity.Property(e => e.EmgCnt3)
                    .HasColumnName("Emg_Cnt_3")
                    .HasMaxLength(15);

                entity.Property(e => e.EmgCntEmail)
                    .HasColumnName("Emg_Cnt_Email")
                    .HasMaxLength(50);

                entity.Property(e => e.EmgCntRelation)
                    .IsRequired()
                    .HasColumnName("Emg_Cnt_Relation")
                    .HasMaxLength(30);

                entity.Property(e => e.EmpId)
                    .IsRequired()
                    .HasColumnName("Emp_Id")
                    .HasMaxLength(10);

                entity.Property(e => e.EmpType)
                    .IsRequired()
                    .HasColumnName("Emp_Type")
                    .HasMaxLength(20);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasColumnName("First_Name")
                    .HasMaxLength(30);

                entity.Property(e => e.Gender)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.GrdId).HasColumnName("Grd_Id");

                entity.Property(e => e.GrpId)
                    .HasColumnName("Grp_Id")
                    .IsRequired(false);

                entity.Property(e => e.HlthCrdExpDate)
                    .HasColumnName("Hlth_Crd_Exp_Date")
                    .HasColumnType("date");

                entity.Property(e => e.HlthCrdNo)
                    .HasColumnName("Hlth_Crd_No")
                    .HasMaxLength(15);

                entity.Property(e => e.HmPhone)
                    .HasColumnName("Hm_Phone")
                    .HasMaxLength(20);

                entity.Property(e => e.JoinedDate)
                    .HasColumnName("Joined_Date")
                    .HasColumnType("date");

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasColumnName("Last_Name")
                    .HasMaxLength(30);

                entity.Property(e => e.LocId).HasColumnName("Loc_Id");

                entity.Property(e => e.MblPhone)
                    .HasColumnName("Mbl_Phone")
                    .HasMaxLength(20);

                entity.Property(e => e.MdlName)
                    .HasColumnName("Mdl_Name")
                    .HasMaxLength(30);

                entity.Property(e => e.MrtlStat)
                    .IsRequired()
                    .HasColumnName("Mrtl_Stat")
                    .HasMaxLength(10);

                entity.Property(e => e.NameArb)
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(150);

                entity.Property(e => e.NltyId).HasColumnName("Nlty_Id");

                entity.Property(e => e.Note).HasMaxLength(300);

                entity.Property(e => e.OtEnabled).HasColumnName("Ot_Enabled");

                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.PmtAddr)
                    .HasColumnName("Pmt_Addr")
                    .HasMaxLength(200);

                entity.Property(e => e.PmtExpDate)
                    .HasColumnName("Pmt_Exp_Date")
                    .HasColumnType("date");

                entity.Property(e => e.PmtId)
                    .IsRequired()
                    .HasColumnName("Pmt_Id")
                    .HasMaxLength(20);

                entity.Property(e => e.PmtTyp)
                    .IsRequired()
                    .HasColumnName("Pmt_Typ")
                    .HasMaxLength(20);

                entity.Property(e => e.PsnEmail)
                    .HasColumnName("Psn_Email")
                    .HasMaxLength(50);

                entity.Property(e => e.PstCntryId).HasColumnName("Pst_Cntry_Id");

                entity.Property(e => e.PstExpDate)
                    .HasColumnName("Pst_Exp_Date")
                    .HasColumnType("date");

                entity.Property(e => e.PstName)
                    .IsRequired()
                    .HasColumnName("Pst_Name")
                    .HasMaxLength(150);

                entity.Property(e => e.PstNo)
                    .HasColumnName("Pst_No")
                    .HasMaxLength(15);

                entity.Property(e => e.PstlCode)
                    .HasColumnName("Pstl_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.RefCod)
                    .HasColumnName("Ref_Cod")
                    .HasMaxLength(10);

                entity.Property(e => e.Regoin).HasMaxLength(10);

                entity.Property(e => e.SeatingLocation)
                    .HasColumnName("Seating_Location")
                    .HasMaxLength(150);

                entity.Property(e => e.SrtName)
                    .HasColumnName("Srt_Name")
                    .HasMaxLength(50);

                entity.Property(e => e.SrtOrd)
                    .HasColumnName("Srt_Ord")
                    .HasDefaultValueSql("((1000))");

                entity.Property(e => e.SrvEndDate)
                    .HasColumnName("Srv_End_Date")
                    .HasColumnType("datetime");

                entity.Property(e => e.TcktTyp)
                    .IsRequired()
                    .HasColumnName("Tckt_Typ")
                    .HasMaxLength(20);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.HasOne(d => d.AddrCntry)
                    .WithMany(p => p.HrEmpGenInfoAddrCntry)
                    .HasForeignKey(d => d.AddrCntryId)
                    .HasConstraintName("FK_HR_EmpGenInfo_ORG_Country_Addr");

                entity.HasOne(d => d.Cntry)
                    .WithMany(p => p.HrEmpGenInfoCntry)
                    .HasForeignKey(d => d.CntryId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpGenInfo_ORG_Country");

                entity.HasOne(d => d.Grd)
                    .WithMany(p => p.HrEmpGenInfo)
                    .HasForeignKey(d => d.GrdId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpGenInfo_HR_EmpGrade");

                entity.HasOne(d => d.Grp)
                    .WithMany(p => p.HrEmpGenInfo)
                    .HasForeignKey(d => d.GrpId)
                    .HasConstraintName("FK_HR_EmpGenInfo_HR_EmpGroup");

                entity.HasOne(d => d.Loc)
                    .WithMany(p => p.HrEmpGenInfo)
                    .HasForeignKey(d => d.LocId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpGenInfo_ORG_Location");

                entity.HasOne(d => d.Nlty)
                    .WithMany(p => p.HrEmpGenInfo)
                    .HasForeignKey(d => d.NltyId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpGenInfo_ORG_Nationality");

                entity.Property(e => e.RelId).HasColumnName("Rel_Id");

                entity.HasOne(d => d.Rel)
                    .WithMany(p => p.HrEmpGenInfo)
                    .HasForeignKey(d => d.RelId)
                    .HasConstraintName("FK_HR_EmpGenInfo_ORG_Religion");

                entity.Property(e => e.WrktypId).HasColumnName("Wrktyp_Id");

                entity.HasOne(d => d.PstCntry)
                    .WithMany(p => p.HrEmpGenInfoPstCntry)
                    .HasForeignKey(d => d.PstCntryId)
                    .HasConstraintName("FK_HR_EmpGenInfo_ORG_Country_Pst");

                entity.HasOne(d => d.Wrktyp)
                    .WithMany(p => p.HrEmpGenInfo)
                    .HasForeignKey(d => d.WrktypId)
                    .HasConstraintName("FK_HR_EmpGenInfo_ORG_WrkType");
            });

            modelBuilder.Entity<HrEmpGrade>(entity =>
            {
                entity.HasKey(e => e.GrdId);

                entity.ToTable("HR_EmpGrade");

                entity.HasIndex(e => e.GrdCode)
                    .IsUnique();

                entity.Property(e => e.GrdId).HasColumnName("Grd_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DescArb)
                    .HasColumnName("Desc_Arb")
                    .HasMaxLength(100);

                entity.Property(e => e.DescEng)
                    .HasColumnName("Desc_Eng")
                    .HasMaxLength(100);

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.GrdCode)
                    .IsRequired()
                    .HasColumnName("Grd_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.TitleArb)
                    .HasColumnName("Title_Arb")
                    .HasMaxLength(30);

                entity.Property(e => e.TitleEng)
                    .IsRequired()
                    .HasColumnName("Title_Eng")
                    .HasMaxLength(30);

                entity.HasOne(d => d.Ent)
                    .WithMany(p => p.HrEmpGrade)
                    .HasForeignKey(d => d.EntId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpGrade_ORG_Entity");
            });

            modelBuilder.Entity<HrEmpGroup>(entity =>
            {
                entity.HasKey(e => e.GrpId);

                entity.ToTable("HR_EmpGroup");

                entity.HasIndex(e => e.GrpCode)
                    .IsUnique();

                entity.Property(e => e.GrpId).HasColumnName("Grp_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DescArb)
                    .HasColumnName("Desc_Arb")
                    .HasMaxLength(100);

                entity.Property(e => e.DescEng)
                    .HasColumnName("Desc_Eng")
                    .HasMaxLength(100);

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.GrpCode)
                    .IsRequired()
                    .HasColumnName("Grp_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.TitleArb)
                    .HasColumnName("Title_Arb")
                    .HasMaxLength(30);

                entity.Property(e => e.TitleEng)
                    .IsRequired()
                    .HasColumnName("Title_Eng")
                    .HasMaxLength(30);

                entity.HasOne(d => d.Ent)
                    .WithMany(p => p.HrEmpGroup)
                    .HasForeignKey(d => d.EntId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpGroup_ORG_Entity");
            });

            modelBuilder.Entity<HrEmpJobAssign>(entity =>
            {
                entity.HasKey(e => e.JbAsngId);

                entity.ToTable("HR_EmpJobAssign");

                entity.Property(e => e.JbAsngId).HasColumnName("Jb_Asng_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.DeptId).HasColumnName("Dept_Id");

                entity.Property(e => e.DesigId).HasColumnName("Desig_Id");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.EmpTypId).HasColumnName("Emp_Typ_Id");

                entity.Property(e => e.EndDate)
                    .HasColumnName("End_Date")
                    .HasColumnType("date");

                entity.Property(e => e.Note)
                    .IsRequired()
                    .HasMaxLength(300);

                entity.Property(e => e.OfcEmail)
                    .HasColumnName("Ofc_Email")
                    .HasMaxLength(50);

                entity.Property(e => e.OfcPhone)
                    .HasColumnName("Ofc_Phone")
                    .HasMaxLength(20);

                entity.Property(e => e.RptMngId).HasColumnName("Rpt_Mng_Id");

                entity.Property(e => e.StDate)
                    .HasColumnName("St_Date")
                    .HasColumnType("date");

                entity.Property(e => e.WkStat).HasColumnName("Wk_Stat");

                entity.HasOne(d => d.Dept)
                    .WithMany(p => p.HrEmpJobAssign)
                    .HasForeignKey(d => d.DeptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpJobAssign_ORG_Department");

                entity.HasOne(d => d.Desig)
                    .WithMany(p => p.HrEmpJobAssign)
                    .HasForeignKey(d => d.DesigId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpJobAssign_ORG_Designation");

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrEmpJobAssign)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpJobAssign_HR_EmpGenInfo");
            });

            modelBuilder.Entity<HrEmpPhoto>(entity =>
            {
                entity.HasKey(e => new { e.EmpSysId, e.PhtId });

                entity.ToTable("HR_EmpPhoto");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.PhtId).HasColumnName("Pht_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.Note).HasMaxLength(100);

                entity.Property(e => e.Photo).IsRequired();

                entity.Property(e => e.PhotoMime)
                    .HasColumnName("Photo_Mime")
                    .HasMaxLength(100);

                entity.Property(e => e.PhotoName)
                    .HasColumnName("Photo_Name")
                    .HasMaxLength(100);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrEmpPhoto)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpPhoto_HR_EmpGenInfo");
            });

            modelBuilder.Entity<HrEmpPreExp>(entity =>
            {
                entity.HasKey(e => e.ExpId);

                entity.ToTable("HR_EmpPreExp");

                entity.Property(e => e.ExpId).HasColumnName("Exp_Id");

                entity.Property(e => e.CompName)
                    .IsRequired()
                    .HasColumnName("Comp_Name")
                    .HasMaxLength(300);

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.EndDate)
                    .HasColumnName("End_Date")
                    .HasColumnType("date");

                entity.Property(e => e.Position)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.StDate)
                    .HasColumnName("St_Date")
                    .HasColumnType("date");

                entity.Property(e => e.SvcCtfct).HasColumnName("Svc_Ctfct");

                entity.Property(e => e.SvcFileMime)
                    .HasColumnName("Svc_File_Mime")
                    .HasMaxLength(100);

                entity.Property(e => e.SvcFileName)
                    .HasColumnName("Svc_File_Name")
                    .HasMaxLength(100);

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrEmpPreExp)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpPreExp_HR_EmpGenInfo");
            });

            modelBuilder.Entity<HrEmpProbation>(entity =>
            {
                entity.HasKey(e => e.ProbId);

                entity.ToTable("HR_EmpProbation");

                entity.Property(e => e.ProbId).HasColumnName("Prob_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.DeptId).HasColumnName("Dept_Id");

                entity.Property(e => e.DesigId).HasColumnName("Desig_Id");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.EndDate)
                    .HasColumnName("End_Date")
                    .HasColumnType("date");

                entity.Property(e => e.IsExtended).HasColumnName("Is_Extended");

                entity.Property(e => e.Note)
                    .IsRequired()
                    .HasMaxLength(300);

                entity.Property(e => e.OfcEmail)
                    .HasColumnName("Ofc_Email")
                    .HasMaxLength(30);

                entity.Property(e => e.OfcPhone)
                    .HasColumnName("Ofc_Phone")
                    .HasMaxLength(20);

                entity.Property(e => e.RptMngId).HasColumnName("Rpt_Mng_Id");

                entity.Property(e => e.StDate)
                    .HasColumnName("St_Date")
                    .HasColumnType("date");

                entity.Property(e => e.WkStat).HasColumnName("Wk_Stat");

                entity.HasOne(d => d.Dept)
                    .WithMany(p => p.HrEmpProbation)
                    .HasForeignKey(d => d.DeptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpProbation_ORG_Department");

                entity.HasOne(d => d.Desig)
                    .WithMany(p => p.HrEmpProbation)
                    .HasForeignKey(d => d.DesigId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpProbation_ORG_Designation");

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrEmpProbation)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpProbation_HR_EmpGenInfo");
            });

            modelBuilder.Entity<HrEmpResidInfo>(entity =>
            {
                entity.HasKey(e => e.ResidId);

                entity.ToTable("HR_EmpResidInfo");

                entity.Property(e => e.ResidId).HasColumnName("Resid_Id");

                entity.Property(e => e.Area)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.BldNo)
                    .HasColumnName("Bld_No")
                    .HasMaxLength(20);

                entity.Property(e => e.CntryId).HasColumnName("Cntry_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.InDate)
                    .HasColumnName("inDate")
                    .HasColumnType("date");

                entity.Property(e => e.MobNo)
                    .HasColumnName("Mob_No")
                    .HasMaxLength(20);

                entity.Property(e => e.Note)
                    .IsRequired()
                    .HasMaxLength(300);

                entity.Property(e => e.OutDate)
                    .HasColumnName("outDate")
                    .HasColumnType("date");

                entity.Property(e => e.PropNo)
                    .HasColumnName("Prop_No")
                    .HasMaxLength(10);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.ResidType)
                    .IsRequired()
                    .HasColumnName("Resid_Type")
                    .HasMaxLength(20);

                entity.Property(e => e.State).HasMaxLength(20);

                entity.Property(e => e.Strt)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.TelNo)
                    .HasColumnName("Tel_No")
                    .HasMaxLength(20);
                
                entity.Property(e => e.IsToDate).HasColumnName("IsToDate");

                entity.HasOne(d => d.Cntry)
                    .WithMany(p => p.HrEmpResidInfo)
                    .HasForeignKey(d => d.CntryId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpResidInfo_ORG_Country");

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrEmpResidInfo)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpResidInfo_HR_EmpGenInfo");
            });

            modelBuilder.Entity<HrEmpSponsor>(entity =>
            {
                entity.HasKey(e => e.SpnsrId);

                entity.ToTable("HR_EmpSponsor");

                entity.Property(e => e.SpnsrId).HasColumnName("Spnsr_Id");

                entity.Property(e => e.CmpDtl)
                    .HasColumnName("Cmp_Dtl")
                    .HasMaxLength(200);

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.EndDate)
                    .HasColumnName("End_Date")
                    .HasColumnType("datetime");

                entity.Property(e => e.EraNo)
                    .HasColumnName("Era_No")
                    .HasMaxLength(20);

                entity.Property(e => e.LbrCard)
                    .HasColumnName("Lbr_Card")
                    .HasMaxLength(20);

                entity.Property(e => e.Note)
                    .HasMaxLength(300);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RltnType)
                    .HasColumnName("Rltn_Type")
                    .HasMaxLength(10);

                entity.Property(e => e.SpnsrType)
                    .IsRequired()
                    .HasColumnName("Spnsr_Type")
                    .HasMaxLength(10);

                entity.Property(e => e.StDate)
                    .HasColumnName("St_Date")
                    .HasColumnType("date");
            });

            modelBuilder.Entity<HrEmpWkRoster>(entity =>
            {
                entity.HasKey(e => e.RecId);

                entity.ToTable("HR_EmpWkRoster");

                entity.Property(e => e.RecId).HasColumnName("Rec_Id");

                entity.Property(e => e.CndrAtndc).HasColumnName("Cndr_Atndc");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.EndDate)
                    .HasColumnName("End_Date")
                    .HasColumnType("datetime");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RstId).HasColumnName("Rst_Id");

                entity.Property(e => e.StDate)
                    .HasColumnName("St_Date")
                    .HasColumnType("datetime");

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrEmpWkRoster)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpWkRoster_HR_EmpGenInfo");

                entity.HasOne(d => d.Rst)
                    .WithMany(p => p.HrEmpWkRoster)
                    .HasForeignKey(d => d.RstId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_EmpWkRoster_HR_WkRoster");
            });

            modelBuilder.Entity<HrWkShift>(entity =>
            {
                entity.HasKey(e => e.SftId);

                entity.ToTable("HR_WkShift");

                entity.HasIndex(e => e.SftCode)
                    .IsUnique();

                entity.Property(e => e.SftId).HasColumnName("Sft_Id");

                entity.Property(e => e.BrkMnt).HasColumnName("Brk_Mnt");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CutOffTimeEnd)
                    .HasColumnName("CutOff_Time_End")
                    .HasColumnType("datetime");

                entity.Property(e => e.CutOffTimeStart)
                    .HasColumnName("CutOff_Time_Start")
                    .HasColumnType("datetime");

                entity.Property(e => e.Desc).HasMaxLength(200);

                entity.Property(e => e.EOtEndTime).HasColumnName("E_Ot_End_Time");

                entity.Property(e => e.EOtIncld).HasColumnName("E_Ot_Incld");

                entity.Property(e => e.EOtMinMnt).HasColumnName("E_Ot_Min_Mnt");

                entity.Property(e => e.EOtStTime).HasColumnName("E_Ot_St_Time");

                entity.Property(e => e.EndTime)
                    .HasColumnName("End_Time")
                    .HasColumnType("datetime");

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.GrcPrd).HasColumnName("Grc_Prd");

                entity.Property(e => e.NOtEndTime).HasColumnName("N_Ot_End_Time");

                entity.Property(e => e.NOtIncld).HasColumnName("N_Ot_Incld");

                entity.Property(e => e.NOtMinMnt).HasColumnName("N_Ot_Min_Mnt");

                entity.Property(e => e.NOtStTime).HasColumnName("N_Ot_St_Time");

                entity.Property(e => e.NameArb)
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(30);

                entity.Property(e => e.NameEng)
                    .IsRequired()
                    .HasColumnName("Name_Eng")
                    .HasMaxLength(30);

                entity.Property(e => e.OffDay).HasColumnName("Off_Day");

                entity.Property(e => e.OvrNghtSft).HasColumnName("Ovr_Nght_Sft");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.SftCode)
                    .IsRequired()
                    .HasColumnName("Sft_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.StTime)
                    .HasColumnName("St_Time")
                    .HasColumnType("datetime");

                entity.HasOne(d => d.Ent)
                    .WithMany(p => p.HrWkShift)
                    .HasForeignKey(d => d.EntId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_WkShift_ORG_Entity");
            });

            modelBuilder.Entity<HrLvAnlSchedule>(entity =>
            {
                entity.HasKey(e => e.LvSdlId);

                entity.ToTable("HR_LvAnlSchedule");

                entity.HasIndex(e => new { e.EmpSysId, e.LvTypId, e.LvYear, e.Incident })
                    .HasName("IX_HR_LvAnlSchedule_UQ0")
                    .IsUnique();

                entity.Property(e => e.LvSdlId).HasColumnName("LvSdl_Id");

                entity.Property(e => e.CrtBy)
                    .HasColumnName("Crt_By")
                    .HasColumnType("datetime");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.EndDate)
                    .HasColumnName("End_Date")
                    .HasColumnType("date");

                entity.Property(e => e.Incident).HasDefaultValueSql("((1))");

                entity.Property(e => e.LvTypId).HasColumnName("LvTyp_Id");

                entity.Property(e => e.LvYear).HasDefaultValueSql("(datepart(year,getdate()))");

                entity.Property(e => e.Note).HasMaxLength(100);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.StDate)
                    .HasColumnName("St_Date")
                    .HasColumnType("date");

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrLvAnlSchedule)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_LvAnlSchedule_HR_EmpGenInfo");

                entity.HasOne(d => d.LvTyp)
                    .WithMany(p => p.HrLvAnlSchedule)
                    .HasForeignKey(d => d.LvTypId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_LvAnlSchedule_HR_LvType");
            });

            modelBuilder.Entity<HrLvRegDocs>(entity =>
            {
                entity.HasKey(e => e.DocId);

                entity.ToTable("HR_LvRegDocs");

                entity.Property(e => e.DocId).HasColumnName("Doc_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.Desc)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.DocMime)
                    .HasColumnName("Doc_Mime")
                    .HasMaxLength(100);

                entity.Property(e => e.DocName)
                    .HasColumnName("Doc_Name")
                    .HasMaxLength(100);

                entity.Property(e => e.LvId).HasColumnName("Lv_Id");

                entity.Property(e => e.ScnCopy).HasColumnName("Scn_Copy");

                entity.HasOne(d => d.Lv)
                    .WithMany(p => p.HrLvRegDocs)
                    .HasForeignKey(d => d.LvId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_LvRegDocs_HR_LvRegister");
            });

            modelBuilder.Entity<HrLvRegister>(entity =>
            {
                entity.HasKey(e => e.LvId);

                entity.ToTable("HR_LvRegister");

                entity.HasIndex(e => new { e.EmpSysId, e.Incident })
                    .HasName("IX_HR_LvRegister_UQ0")
                    .IsUnique();

                entity.Property(e => e.LvId).HasColumnName("Lv_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.CurrAvblDays).HasColumnName("Curr_Avbl_Days");

                entity.Property(e => e.DocId).HasColumnName("Doc_Id");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.EndDate)
                    .HasColumnName("End_Date")
                    .HasColumnType("date");

                entity.Property(e => e.Incident).HasDefaultValueSql("((1))");

                entity.Property(e => e.LvSdlId).HasColumnName("LvSdl_Id");

                entity.Property(e => e.LvTypId).HasColumnName("LvTyp_Id");

                entity.Property(e => e.LvYear).HasDefaultValueSql("(datepart(year,getdate()))");

                entity.Property(e => e.Note).HasMaxLength(100);

                entity.Property(e => e.RecStat).HasColumnName("Rec_Stat");

                entity.Property(e => e.RjnDate)
                    .HasColumnName("Rjn_Date")
                    .HasColumnType("date");

                entity.Property(e => e.RjnStat)
                    .HasColumnName("Rjn_Stat")
                    .HasDefaultValueSql("((-1))");

                entity.Property(e => e.StDate)
                    .HasColumnName("St_Date")
                    .HasColumnType("date");

                // ── Workflow Integration columns ──────────────────────────────
                entity.Property(e => e.WfStat)
                    .HasColumnName("WfStat")
                    .HasMaxLength(30)
                    .IsRequired(false);

                entity.Property(e => e.WfInstId)
                    .HasColumnName("WfInstId")
                    .IsRequired(false);
                // ─────────────────────────────────────────────────────────────

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrLvRegister)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_LvRegister_HR_EmpGenInfo");

                entity.HasOne(d => d.LvSdl)
                    .WithMany(p => p.HrLvRegister)
                    .HasForeignKey(d => d.LvSdlId)
                    .HasConstraintName("FK_HR_LvRegister_HR_LvAnlSchedule");

                entity.HasOne(d => d.LvTyp)
                    .WithMany(p => p.HrLvRegister)
                    .HasForeignKey(d => d.LvTypId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_LvRegister_HR_LvType");
            });

            modelBuilder.Entity<HrLvType>(entity =>
            {
                entity.HasKey(e => e.LvTypId);

                entity.ToTable("HR_LvType");

                entity.HasIndex(e => e.LvTypCode)
                    .IsUnique();

                entity.Property(e => e.LvTypId).HasColumnName("LvTyp_Id");

                entity.Property(e => e.AprlRqd).HasColumnName("Aprl_Rqd");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Desc).HasMaxLength(200);

                entity.Property(e => e.ElgDays)
                    .HasColumnName("Elg_Days")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.LvTypCode)
                    .IsRequired()
                    .HasColumnName("LvTyp_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.MaxCntDays)
                    .HasColumnName("Max_CntDays")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.NameArb)
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(20);

                entity.Property(e => e.NameEng)
                    .IsRequired()
                    .HasColumnName("Name_Eng")
                    .HasMaxLength(20);

                entity.Property(e => e.PreShld).HasColumnName("Pre_Shld");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RjnFmRqd).HasColumnName("Rjn_Fm_Rqd");

                entity.Property(e => e.SlryDctFct)
                    .HasColumnName("Slry_Dct_Fct")
                    .HasColumnType("decimal(3, 2)");

                entity.HasOne(d => d.Ent)
                    .WithMany(p => p.HrLvType)
                    .HasForeignKey(d => d.EntId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_LvType_ORG_Entity");
            });

            modelBuilder.Entity<HrLvPolicy>(entity =>
            {
                entity.HasKey(e => e.PlcyId);

                entity.ToTable("HR_LvPolicy");

                entity.Property(e => e.PlcyId).HasColumnName("Plcy_Id");

                entity.Property(e => e.LvTypId).HasColumnName("LvTyp_Id");

                entity.Property(e => e.Desc)
                    .HasColumnName("Desc")
                    .HasMaxLength(200);

                entity.Property(e => e.AppAfterDays)
                    .HasColumnName("App_After_Days")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.ElgDays)
                    .HasColumnName("Elg_Days")
                    .HasColumnType("decimal(5, 2)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.FullPaidDays)
                    .HasColumnName("Full_Paid_Days")
                    .HasColumnType("decimal(5, 2)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.HalfPaidDays)
                    .HasColumnName("Half_Paid_Days")
                    .HasColumnType("decimal(5, 2)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.NoPayDays)
                    .HasColumnName("No_Pay_Days")
                    .HasColumnType("decimal(5, 2)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.IsNoPay)
                    .HasColumnName("Is_No_Pay")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.PrdUnit)
                    .HasColumnName("Prd_Unit")
                    .HasMaxLength(20)
                    .HasDefaultValueSql("('YEAR')");

                entity.Property(e => e.EntId)
                    .HasColumnName("Ent_Id")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtBy)
                    .HasColumnName("Crt_By")
                    .HasMaxLength(50);

                entity.Property(e => e.ModDateTime)
                    .HasColumnName("Mod_DateTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.ModBy)
                    .HasColumnName("Mod_By")
                    .HasMaxLength(50);

                entity.HasOne(d => d.LvTyp)
                    .WithMany(p => p.HrLvPolicy)
                    .HasForeignKey(d => d.LvTypId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_LvPolicy_HR_LvType");
            });

            modelBuilder.Entity<HrPrAdHocTrnType>(entity =>
            {
                entity.HasKey(e => e.TrnTypId);

                entity.ToTable("HR_PrAdHocTrnType");

                entity.Property(e => e.TrnTypId).HasColumnName("Trn_Typ_Id");

                entity.Property(e => e.AutoGen).HasColumnName("Auto_Gen");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Desc).HasMaxLength(200);

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.TrnTypTxt)
                    .IsRequired()
                    .HasColumnName("Trn_Typ_Txt")
                    .HasMaxLength(15);

                entity.Property(e => e.TrnType)
                    .IsRequired()
                    .HasColumnName("Trn_Type")
                    .HasMaxLength(6);

                entity.HasOne(d => d.Ent)
                    .WithMany(p => p.HrPrAdHocTrnType)
                    .HasForeignKey(d => d.EntId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrAdHocTrnType_ORG_Entity");
            });

            modelBuilder.Entity<HrPrAdHocTrns>(entity =>
            {
                entity.HasKey(e => e.TrnId);

                entity.ToTable("HR_PrAdHocTrns");

                entity.Property(e => e.TrnId).HasColumnName("Trn_Id");

                entity.Property(e => e.CrtBy).HasColumnName("Crt_By");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Desc)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.EftMonth).HasColumnName("Eft_Month");

                entity.Property(e => e.EftYear).HasColumnName("Eft_Year");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.PrcLogId).HasColumnName("PrcLog_Id");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.Ref)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.ShtRmk)
                    .HasColumnName("Sht_Rmk")
                    .HasMaxLength(35);

                entity.Property(e => e.TrnAmt)
                    .HasColumnName("Trn_Amt")
                    .HasColumnType("decimal(9, 2)");

                entity.Property(e => e.TrnDate)
                    .HasColumnName("Trn_Date")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.TrnTypId).HasColumnName("Trn_Typ_Id");

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrPrAdHocTrns)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrAdHocTrns_HR_EmpGenInfo");

                entity.HasOne(d => d.PrcLog)
                    .WithMany(p => p.HrPrAdHocTrns)
                    .HasForeignKey(d => d.PrcLogId)
                    .HasConstraintName("FK_HR_PrAdHocTrns_HR_PrSlryPrcLog");

                entity.HasOne(d => d.TrnTyp)
                    .WithMany(p => p.HrPrAdHocTrns)
                    .HasForeignKey(d => d.TrnTypId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrAdHocTrns_HR_PrAdHocTrnType");
            });

            modelBuilder.Entity<HrPrCnfGrtvtyLvl>(entity =>
            {
                entity.HasKey(e => e.GrtLvlId);

                entity.ToTable("HR_PrCnfGrtvtyLvl");

                entity.Property(e => e.GrtLvlId).HasColumnName("Grt_Lvl_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.Desc).HasMaxLength(100);

                entity.Property(e => e.EftDays).HasColumnName("Eft_Days");

                entity.Property(e => e.LwrBndryYrs).HasColumnName("Lwr_Bndry_Yrs");

                entity.Property(e => e.PrCnfId).HasColumnName("Pr_Cnf_Id");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.UprBndryYrs).HasColumnName("Upr_Bndry_Yrs");

                entity.HasOne(d => d.PrCnf)
                    .WithMany(p => p.HrPrCnfGrtvtyLvl)
                    .HasForeignKey(d => d.PrCnfId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrCnfGrtvtyLvl_HR_PrConfig");
            });

            modelBuilder.Entity<HrPrConfig>(entity =>
            {
                entity.HasKey(e => e.PrCnfId);

                entity.ToTable("HR_PrConfig");

                entity.Property(e => e.PrCnfId).HasColumnName("Pr_Cnf_Id");

                entity.Property(e => e.AttnCtDay)
                    .HasColumnName("Attn_Ct_Day")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.AttnCtEom).HasColumnName("Attn_Ct_EOM");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.EsGrtActDiM).HasColumnName("Es_Grt_Act_DiM");

                entity.Property(e => e.EsGrtActDiY).HasColumnName("Es_Grt_Act_DiY");

                entity.Property(e => e.EsGrtDiMVal).HasColumnName("Es_Grt_DiM_Val");

                entity.Property(e => e.EsGrtDiYVal).HasColumnName("Es_Grt_DiY_Val");

                entity.Property(e => e.EsLmsActDiM).HasColumnName("Es_LMS_Act_DiM");

                entity.Property(e => e.EsLmsDiMVal).HasColumnName("Es_LMS_DiM_Val");

                entity.Property(e => e.EsLvSActDiY).HasColumnName("Es_LvS_Act_DiY");

                entity.Property(e => e.EsLvSDiYVal).HasColumnName("Es_LvS_DiY_Val");

                entity.Property(e => e.EsLvSMnWkPrd).HasColumnName("Es_LvS_Mn_Wk_Prd");

                entity.Property(e => e.Ext1).HasMaxLength(50);

                entity.Property(e => e.Ext2).HasMaxLength(50);

                entity.Property(e => e.Ext3).HasMaxLength(50);

                entity.Property(e => e.Notes).HasMaxLength(200);

                entity.Property(e => e.OtClcActMntHrs).HasColumnName("Ot_Clc_Act_Mnt_Hrs");

                entity.Property(e => e.OtClcMntHrsVal).HasColumnName("Ot_Clc_Mnt_Hrs_Val");

                entity.Property(e => e.PyrBnkNm)
                    .HasColumnName("Pyr_Bnk_Nm")
                    .HasMaxLength(100);

                entity.Property(e => e.PyrBnkShtNm)
                    .IsRequired()
                    .HasColumnName("Pyr_Bnk_Sht_Nm")
                    .HasMaxLength(10);

                entity.Property(e => e.PyrBrnch)
                    .HasColumnName("Pyr_Brnch")
                    .HasMaxLength(50);

                entity.Property(e => e.PyrEid)
                    .IsRequired()
                    .HasColumnName("Pyr_EID")
                    .HasMaxLength(50);

                entity.Property(e => e.PyrIban)
                    .IsRequired()
                    .HasColumnName("Pyr_IBAN")
                    .HasMaxLength(50);

                entity.Property(e => e.PyrQid)
                    .IsRequired()
                    .HasColumnName("Pyr_QID")
                    .HasMaxLength(50);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.SlryPrcDay)
                    .HasColumnName("Slry_Prc_Day")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.SlryPrcEom).HasColumnName("Slry_Prc_EOM");

                entity.Property(e => e.SlryPrcLstMnt).HasColumnName("Slry_Prc_Lst_Mnt");

                entity.Property(e => e.SlryPrcStMonth).HasColumnName("Slry_Prc_St_Month");

                entity.Property(e => e.SlryPrcStYear).HasColumnName("Slry_Prc_St_Year");

                entity.HasOne(d => d.Ent)
                    .WithMany(p => p.HrPrConfig)
                    .HasForeignKey(d => d.EntId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrConfig_ORG_Entity");
            });

            modelBuilder.Entity<HrPrEmpSlryStruc>(entity =>
            {
                entity.HasKey(e => e.SlStrcId);

                entity.ToTable("HR_PrEmpSlryStruc");

                entity.Property(e => e.SlStrcId).HasColumnName("SlStrc_Id");

                entity.Property(e => e.Basic).IsRequired();

                entity.Property(e => e.BonusRate)
                    .IsRequired()
                    .HasColumnName("Bonus_Rate");

                entity.Property(e => e.CrtBy).HasColumnName("Crt_By");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.HrAlwnc)
                    .IsRequired()
                    .HasColumnName("HR_Alwnc");

                entity.Property(e => e.IncntvPrc)
                    .IsRequired()
                    .HasColumnName("Incntv_Prc");

                entity.Property(e => e.MedAlwnc)
                    .IsRequired()
                    .HasColumnName("Med_Alwnc");

                entity.Property(e => e.MobAlwnc)
                    .IsRequired()
                    .HasColumnName("Mob_Alwnc");

                entity.Property(e => e.OtId).HasColumnName("Ot_Id");

                entity.Property(e => e.OthrAlwnc)
                    .IsRequired()
                    .HasColumnName("Othr_Alwnc");

                entity.Property(e => e.PrfmAlwnc)
                    .IsRequired()
                    .HasColumnName("Prfm_Alwnc");

                entity.Property(e => e.PstnAlwnc)
                    .IsRequired()
                    .HasColumnName("Pstn_Alwnc");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.SpcAlwnc)
                    .IsRequired()
                    .HasColumnName("Spc_Alwnc");

                entity.Property(e => e.Tra)
                    .IsRequired()
                    .HasColumnName("TRA");

                entity.Property(e => e.TrnsAlwnc)
                    .IsRequired()
                    .HasColumnName("Trns_Alwnc");

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrPrEmpSlryStruc)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrEmpSlryStruc_HR_EmpGenInfo");

                entity.HasOne(d => d.Ot)
                    .WithMany(p => p.HrPrEmpSlryStruc)
                    .HasForeignKey(d => d.OtId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrEmpSlryStruc_HR_PrOTRate");
            });

            modelBuilder.Entity<HrPrInctv>(entity =>
            {
                entity.HasKey(e => e.InctvId);

                entity.ToTable("HR_PrInctv");

                entity.Property(e => e.InctvId).HasColumnName("Inctv_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.Desc)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.EftMonth).HasColumnName("Eft_Month");

                entity.Property(e => e.EftYear).HasColumnName("Eft_Year");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.InctvAmt)
                    .HasColumnName("Inctv_Amt")
                    .HasColumnType("decimal(9, 2)");

                entity.Property(e => e.InctvDate)
                    .HasColumnName("Inctv_Date")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.InctvMod).HasColumnName("Inctv_mod");

                entity.Property(e => e.InctvTyp).HasColumnName("Inctv_Typ");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.Ref)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.ShtRmk)
                    .HasColumnName("Sht_Rmk")
                    .HasMaxLength(35);

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrPrInctv)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrInctv_HR_EmpGenInfo");
            });

            modelBuilder.Entity<HrPrLvEncsh>(entity =>
            {
                entity.HasKey(e => e.LvEncshId);

                entity.ToTable("HR_PrLvEncsh");

                entity.Property(e => e.LvEncshId).HasColumnName("LvEncsh_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.EftWrkDys).HasColumnName("Eft_Wrk_Dys");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.ErndLvCnt).HasColumnName("Ernd_Lv_Cnt");

                entity.Property(e => e.FrmDt)
                    .HasColumnName("Frm_Dt")
                    .HasColumnType("datetime");

                entity.Property(e => e.FrmDtTyp)
                    .IsRequired()
                    .HasColumnName("Frm_Dt_Typ")
                    .HasMaxLength(2);

                entity.Property(e => e.GrsSlry)
                    .HasColumnName("Grs_Slry")
                    .HasColumnType("decimal(9, 2)");

                entity.Property(e => e.IncldLstMnthSlry).HasColumnName("Incld_Lst_Mnth_Slry");

                entity.Property(e => e.IncldPrjAlwnc).HasColumnName("Incld_Prj_Alwnc");

                entity.Property(e => e.IncldTktFee).HasColumnName("Incld_Tkt_Fee");

                entity.Property(e => e.LstMntSlry)
                    .HasColumnName("Lst_Mnt_Slry")
                    .HasColumnType("decimal(9, 2)");

                entity.Property(e => e.LvSlry)
                    .HasColumnName("Lv_Slry")
                    .HasColumnType("decimal(9, 2)");

                entity.Property(e => e.NpCnt).HasColumnName("Np_Cnt");

                entity.Property(e => e.PlcyId).HasColumnName("Plcy_Id");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RefId).HasColumnName("Ref_Id");

                entity.Property(e => e.RefTyp)
                    .IsRequired()
                    .HasColumnName("Ref_Typ")
                    .HasMaxLength(3);

                entity.Property(e => e.Rmks).HasMaxLength(300);

                entity.Property(e => e.StatIndx).HasColumnName("Stat_Indx");

                entity.Property(e => e.ToDt)
                    .HasColumnName("To_Dt")
                    .HasColumnType("datetime");

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrPrLvEncsh)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrLvEncsh_HR_EmpGenInfo");
            });

            modelBuilder.Entity<HrPrOtrate>(entity =>
            {
                entity.HasKey(e => e.OtId);

                entity.ToTable("HR_PrOTRate");

                entity.HasIndex(e => e.OtCode)
                    .IsUnique();

                entity.Property(e => e.OtId).HasColumnName("Ot_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DescArb)
                    .HasColumnName("Desc_Arb")
                    .HasMaxLength(50);

                entity.Property(e => e.DescEng)
                    .IsRequired()
                    .HasColumnName("Desc_Eng")
                    .HasMaxLength(50);

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.ExtRate)
                    .HasColumnName("Ext_Rate")
                    .HasColumnType("decimal(4, 2)")
                    .HasDefaultValueSql("((0.00))");

                entity.Property(e => e.NmlRate)
                    .HasColumnName("Nml_Rate")
                    .HasColumnType("decimal(4, 2)")
                    .HasDefaultValueSql("((0.00))");

                entity.Property(e => e.OnlyBasic).HasColumnName("Only_Basic");

                entity.Property(e => e.OtCode)
                    .IsRequired()
                    .HasColumnName("Ot_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.HasOne(d => d.Ent)
                    .WithMany(p => p.HrPrOtrate)
                    .HasForeignKey(d => d.EntId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrOTRate_ORG_Entity");
            });

            modelBuilder.Entity<HrPrProjAlwnc>(entity =>
            {
                entity.HasKey(e => e.ProjAlwncId);

                entity.ToTable("HR_PrProjAlwnc");

                entity.Property(e => e.ProjAlwncId).HasColumnName("ProjAlwnc_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.EftMonth).HasColumnName("Eft_Month");

                entity.Property(e => e.EftYear).HasColumnName("Eft_Year");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.EndMonth).HasColumnName("End_Month");

                entity.Property(e => e.EndYear).HasColumnName("End_Year");

                entity.Property(e => e.Note).HasMaxLength(200);

                entity.Property(e => e.ProjAlwncAmt)
                    .HasColumnName("ProjAlwnc_Amt")
                    .HasColumnType("decimal(9, 2)");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.Ref)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.ShtRmk)
                    .HasColumnName("Sht_Rmk")
                    .HasMaxLength(15);

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrPrProjAlwnc)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrProjAlwnc_HR_EmpGenInfo");
            });

            modelBuilder.Entity<HrPrSlryAtnAdj>(entity =>
            {
                entity.HasKey(e => e.AtnAdjId);

                entity.ToTable("HR_PrSlryAtnAdj");

                entity.Property(e => e.AtnAdjId).HasColumnName("Atn_Adj_Id");

                entity.Property(e => e.AdjAmt)
                    .HasColumnName("Adj_Amt")
                    .HasColumnType("decimal(9, 2)");

                entity.Property(e => e.AtnAdjTyp)
                    .IsRequired()
                    .HasColumnName("Atn_Adj_Typ")
                    .HasMaxLength(15);

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.Fct).HasColumnType("decimal(4, 2)");

                entity.Property(e => e.PrcId).HasColumnName("Prc_Id");

                entity.Property(e => e.PrcRvsn).HasColumnName("Prc_Rvsn");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.Remarks)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.RmkExt)
                    .HasColumnName("Rmk_Ext")
                    .HasMaxLength(30);

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrPrSlryAtnAdj)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrSlryAtnAdj_HR_EmpGenInfo");

                entity.HasOne(d => d.Prc)
                    .WithMany(p => p.HrPrSlryAtnAdj)
                    .HasForeignKey(d => d.PrcId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrSlryAtnAdj_HR_PrSlryPrcHdr");
            });

            modelBuilder.Entity<HrPrSlryPrcDtlPsv>(entity =>
            {
                entity.HasKey(e => e.PrcPsvRecId);

                entity.ToTable("HR_PrSlryPrcDtl_Psv");

                entity.Property(e => e.PrcPsvRecId).HasColumnName("PrcPsvRec_Id");

                entity.Property(e => e.AtnPmtAmt)
                    .IsRequired()
                    .HasColumnName("Atn_Pmt_Amt");

                entity.Property(e => e.AtnRcvryAmt)
                    .IsRequired()
                    .HasColumnName("Atn_Rcvry_Amt");

                entity.Property(e => e.Basic).IsRequired();

                entity.Property(e => e.BnkAcct)
                    .IsRequired()
                    .HasColumnName("Bnk_Acct")
                    .HasMaxLength(35);

                entity.Property(e => e.BonusAmt)
                    .IsRequired()
                    .HasColumnName("Bonus_Amt");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.EmpStat).HasColumnName("Emp_Stat");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.HrAlwnc)
                    .IsRequired()
                    .HasColumnName("HR_Alwnc");

                entity.Property(e => e.IncntvAmt)
                    .IsRequired()
                    .HasColumnName("Incntv_Amt");

                entity.Property(e => e.MedAlwnc)
                    .IsRequired()
                    .HasColumnName("Med_Alwnc");

                entity.Property(e => e.MobAlwnc)
                    .IsRequired()
                    .HasColumnName("Mob_Alwnc");

                entity.Property(e => e.NpAmt)
                    .IsRequired()
                    .HasColumnName("Np_Amt");

                entity.Property(e => e.OtAmt)
                    .IsRequired()
                    .HasColumnName("Ot_Amt");

                entity.Property(e => e.OthrAlwnc)
                    .IsRequired()
                    .HasColumnName("Othr_Alwnc");

                entity.Property(e => e.OthrPmt)
                    .IsRequired()
                    .HasColumnName("Othr_Pmt");

                entity.Property(e => e.OthrRcvry)
                    .IsRequired()
                    .HasColumnName("Othr_Rcvry");

                entity.Property(e => e.PmtMod).HasColumnName("Pmt_Mod");

                entity.Property(e => e.PrcId).HasColumnName("Prc_Id");

                entity.Property(e => e.PrcRvsn)
                    .HasColumnName("Prc_Rvsn")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.PrfmAlwnc)
                    .IsRequired()
                    .HasColumnName("Prfm_Alwnc");

                entity.Property(e => e.ProjAlwnc)
                    .IsRequired()
                    .HasColumnName("Proj_Alwnc");

                entity.Property(e => e.PstnAlwnc)
                    .IsRequired()
                    .HasColumnName("Pstn_Alwnc");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.SlryAdv)
                    .IsRequired()
                    .HasColumnName("Slry_Adv");

                entity.Property(e => e.SpcAlwnc)
                    .IsRequired()
                    .HasColumnName("Spc_Alwnc");

                entity.Property(e => e.StfLnPmt)
                    .IsRequired()
                    .HasColumnName("Stf_Ln_Pmt");

                entity.Property(e => e.Tra)
                    .IsRequired()
                    .HasColumnName("TRA");

                entity.Property(e => e.TrnsAlwnc)
                    .IsRequired()
                    .HasColumnName("Trns_Alwnc");

                entity.Property(e => e.WkDayCnt)
                    .HasColumnName("Wk_Day_Cnt")
                    .HasColumnType("decimal(4, 2)");

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrPrSlryPrcDtlPsv)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrSlryPrcDtl_Psv_HR_EmpGenInfo");

                entity.HasOne(d => d.Prc)
                    .WithMany(p => p.HrPrSlryPrcDtlPsv)
                    .HasForeignKey(d => d.PrcId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrSlryPrcDtl_Psv_HR_PrSlryPrcHdr");
            });

            modelBuilder.Entity<HrPrSlryPrcExcld>(entity =>
            {
                entity.HasKey(e => e.ExcldId);

                entity.ToTable("HR_PrSlryPrcExcld");

                entity.Property(e => e.ExcldId).HasColumnName("Excld_Id");

                entity.Property(e => e.CrtBy).HasColumnName("Crt_By");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.NoMnths).HasColumnName("No_Mnths");

                entity.Property(e => e.Note).HasMaxLength(200);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.StMonth).HasColumnName("St_Month");

                entity.Property(e => e.StYear).HasColumnName("St_Year");

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrPrSlryPrcExcld)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrSlryPrcExcld_HR_EmpGenInfo");
            });

            modelBuilder.Entity<HrPrSlryPrcHdr>(entity =>
            {
                entity.HasKey(e => e.PrcId);

                entity.ToTable("HR_PrSlryPrcHdr");

                entity.Property(e => e.PrcId).HasColumnName("Prc_Id");

                entity.Property(e => e.CmpdBy).HasColumnName("Cmpd_By");

                entity.Property(e => e.CmpdDateTime)
                    .HasColumnName("Cmpd_DateTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.EftMonth).HasColumnName("Eft_Month");

                entity.Property(e => e.EftYear).HasColumnName("Eft_Year");

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.InitBy).HasColumnName("Init_By");

                entity.Property(e => e.InitDateTime)
                    .HasColumnName("Init_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.PrcStat)
                    .HasColumnName("Prc_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.VrfdBy).HasColumnName("Vrfd_By");

                entity.Property(e => e.VrfdDateTime)
                    .HasColumnName("Vrfd_DateTime")
                    .HasColumnType("datetime");

                entity.HasOne(d => d.Ent)
                    .WithMany(p => p.HrPrSlryPrcHdr)
                    .HasForeignKey(d => d.EntId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrSlryPrcHdr_ORG_Entity");
            });

            modelBuilder.Entity<HrPrSlryPrcLog>(entity =>
            {
                entity.HasKey(e => e.PrcLogId)
                    .HasName("PK_HR_PrSlryPrcRev");

                entity.ToTable("HR_PrSlryPrcLog");

                entity.Property(e => e.PrcLogId).HasColumnName("PrcLog_Id");

                entity.Property(e => e.CrtBy).HasColumnName("Crt_By");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Note)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.PrcId).HasColumnName("Prc_Id");

                entity.Property(e => e.PrcRvsn).HasColumnName("Prc_Rvsn");

                entity.Property(e => e.PrcTyp)
                    .IsRequired()
                    .HasColumnName("Prc_Typ")
                    .HasMaxLength(3);

                entity.Property(e => e.RevStat).HasColumnName("Rev_Stat");

                entity.HasOne(d => d.Prc)
                    .WithMany(p => p.HrPrSlryPrcLog)
                    .HasForeignKey(d => d.PrcId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrSlryPrcLog_HR_PrSlryPrcHdr");
            });

            modelBuilder.Entity<HrPrSlryPrcLogDtl>(entity =>
            {
                entity.HasKey(e => e.PrcLogDtlId);

                entity.ToTable("HR_PrSlryPrcLogDtl");

                entity.Property(e => e.PrcLogDtlId).HasColumnName("PrcLogDtl_Id");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.PrcLogId).HasColumnName("PrcLog_Id");

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrPrSlryPrcLogDtl)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrSlryPrcLogDtl_HR_EmpGenInfo");

                entity.HasOne(d => d.PrcLog)
                    .WithMany(p => p.HrPrSlryPrcLogDtl)
                    .HasForeignKey(d => d.PrcLogId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrSlryPrcLogDtl_HR_PrSlryPrcLog");
            });

            modelBuilder.Entity<HrPrStatutoryCmplnc>(entity =>
            {
                entity.HasKey(e => e.StutryId);

                entity.ToTable("HR_PrStatutoryCmplnc");

                entity.HasIndex(e => e.StutryCode)
                    .IsUnique();

                entity.Property(e => e.StutryId).HasColumnName("Stutry_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Desc).HasMaxLength(200);

                entity.Property(e => e.DescArb)
                    .HasColumnName("Desc_Arb")
                    .HasMaxLength(200);

                entity.Property(e => e.IsPrcnt).HasColumnName("Is_Prcnt");

                entity.Property(e => e.MaxSlry)
                    .HasColumnName("Max_Slry")
                    .HasColumnType("decimal(9, 2)")
                    .HasDefaultValueSql("((1000000.00))");

                entity.Property(e => e.MinSlry)
                    .HasColumnName("Min_Slry")
                    .HasColumnType("decimal(9, 2)");

                entity.Property(e => e.NameArb)
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(20);

                entity.Property(e => e.NameEng)
                    .IsRequired()
                    .HasColumnName("Name_Eng")
                    .HasMaxLength(20);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.StutryCode)
                    .IsRequired()
                    .HasColumnName("Stutry_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.Value).HasColumnType("decimal(9, 2)");
            });

            modelBuilder.Entity<HrPrStfLoan>(entity =>
            {
                entity.HasKey(e => e.StfLnId);

                entity.ToTable("HR_PrStfLoan");

                entity.HasIndex(e => e.LnCode)
                    .IsUnique();

                entity.Property(e => e.StfLnId).HasColumnName("StfLn_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.EftMonth).HasColumnName("Eft_Month");

                entity.Property(e => e.EftYear).HasColumnName("Eft_Year");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.FlAmt)
                    .HasColumnName("Fl_Amt")
                    .HasColumnType("decimal(9, 2)");

                entity.Property(e => e.Instl).HasDefaultValueSql("((1))");

                entity.Property(e => e.InstlAmt)
                    .HasColumnName("Instl_Amt")
                    .HasColumnType("decimal(9, 2)");

                entity.Property(e => e.IssuDate)
                    .HasColumnName("Issu_Date")
                    .HasColumnType("date")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.LnCode)
                    .IsRequired()
                    .HasColumnName("Ln_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.LnTypId).HasColumnName("LnTyp_Id");

                entity.Property(e => e.Note).HasMaxLength(300);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RefStfLnId).HasColumnName("Ref_StfLn_Id");

                entity.Property(e => e.RmAmt)
                    .HasColumnName("Rm_Amt")
                    .HasColumnType("decimal(9, 2)");

                entity.Property(e => e.ShtRmk)
                    .HasColumnName("Sht_Rmk")
                    .HasMaxLength(15);

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrPrStfLoan)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrStfLoan_HR_EmpGenInfo");

                entity.HasOne(d => d.LnTyp)
                    .WithMany(p => p.HrPrStfLoan)
                    .HasForeignKey(d => d.LnTypId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrStfLoan_HR_PrStfLoanType");

                entity.HasOne(d => d.RefStfLn)
                    .WithMany(p => p.InverseRefStfLn)
                    .HasForeignKey(d => d.RefStfLnId)
                    .HasConstraintName("FK_HR_PrStfLoan_HR_PrStfLoan_Ref");
            });

            modelBuilder.Entity<HrPrStfLoanPmt>(entity =>
            {
                entity.HasKey(e => e.LnPmtId);

                entity.ToTable("HR_PrStfLoanPmt");

                entity.Property(e => e.LnPmtId).HasColumnName("LnPmt_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.LnPmtExpId).HasColumnName("LnPmtExp_Id");

                entity.Property(e => e.Note).HasMaxLength(300);

                entity.Property(e => e.PmtAmt)
                    .HasColumnName("Pmt_Amt")
                    .HasColumnType("decimal(9, 2)");

                entity.Property(e => e.PmtDate)
                    .HasColumnName("Pmt_Date")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.PmtMod).HasColumnName("Pmt_Mod");

                entity.Property(e => e.PrcId).HasColumnName("Prc_Id");

                entity.Property(e => e.PrcMonth).HasColumnName("Prc_Month");

                entity.Property(e => e.PrcRvsn).HasColumnName("Prc_Rvsn");

                entity.Property(e => e.PrcYear).HasColumnName("Prc_Year");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.StfLnId).HasColumnName("StfLn_Id");

                entity.HasOne(d => d.LnPmtExp)
                    .WithMany(p => p.HrPrStfLoanPmt)
                    .HasForeignKey(d => d.LnPmtExpId)
                    .HasConstraintName("FK_HR_PrStfLoanPmt_HR_PrStfLoanPmtExp");

                entity.HasOne(d => d.Prc)
                    .WithMany(p => p.HrPrStfLoanPmt)
                    .HasForeignKey(d => d.PrcId)
                    .HasConstraintName("FK_HR_PrStfLoanPmt_HR_PrSlryPrcHdr");

                entity.HasOne(d => d.StfLn)
                    .WithMany(p => p.HrPrStfLoanPmt)
                    .HasForeignKey(d => d.StfLnId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrStfLoanPmt_HR_PrStfLoan");
            });

            modelBuilder.Entity<HrPrStfLoanPmtExp>(entity =>
            {
                entity.HasKey(e => e.LnPmtExpId);

                entity.ToTable("HR_PrStfLoanPmtExp");

                entity.Property(e => e.LnPmtExpId).HasColumnName("LnPmtExp_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.EftMonth).HasColumnName("Eft_Month");

                entity.Property(e => e.EftYear).HasColumnName("Eft_Year");

                entity.Property(e => e.EntryDate)
                    .HasColumnName("Entry_Date")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.ExpInstl)
                    .HasColumnName("Exp_Instl")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.ExpPmtAmt)
                    .HasColumnName("Exp_Pmt_Amt")
                    .HasColumnType("decimal(9, 2)");

                entity.Property(e => e.Note)
                    .IsRequired()
                    .HasMaxLength(300);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.StfLnId).HasColumnName("StfLn_Id");

                entity.HasOne(d => d.StfLn)
                    .WithMany(p => p.HrPrStfLoanPmtExp)
                    .HasForeignKey(d => d.StfLnId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrStfLoanPmtExp_HR_PrStfLoan");
            });

            modelBuilder.Entity<HrPrStfLoanType>(entity =>
            {
                entity.HasKey(e => e.LnTypId);

                entity.ToTable("HR_PrStfLoanType");

                entity.HasIndex(e => e.LnTypCode)
                    .HasName("IX_HR_PrStfLoanType_Plcy_Code")
                    .IsUnique();

                entity.Property(e => e.LnTypId).HasColumnName("LnTyp_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Desc).HasMaxLength(100);

                entity.Property(e => e.DescArb)
                    .HasColumnName("Desc_Arb")
                    .HasMaxLength(100);

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.LnTypCode)
                    .IsRequired()
                    .HasColumnName("LnTyp_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.MaxAmt)
                    .HasColumnName("Max_Amt")
                    .HasColumnType("decimal(9, 2)");

                entity.Property(e => e.MaxInstl)
                    .HasColumnName("Max_Instl")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.SlryAdv).HasColumnName("Slry_Adv");

                entity.HasOne(d => d.Ent)
                    .WithMany(p => p.HrPrStfLoanType)
                    .HasForeignKey(d => d.EntId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_PrStfLoanType_ORG_Entity");
            });

            modelBuilder.Entity<HrTrvlTktDtl>(entity =>
            {
                entity.HasKey(e => e.TrvlTktId);

                entity.ToTable("HR_TrvlTktDtl");

                entity.Property(e => e.TrvlTktId).HasColumnName("TrvlTkt_Id");

                entity.Property(e => e.AprdTktAmt)
                    .HasColumnName("Aprd_Tkt_Amt")
                    .HasColumnType("decimal(9, 2)");

                entity.Property(e => e.CmpTkt).HasColumnName("Cmp_Tkt");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.DpndId).HasColumnName("Dpnd_Id");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.RecStat).HasColumnName("Rec_Stat");

                entity.Property(e => e.RefId).HasColumnName("Ref_Id");

                entity.Property(e => e.RefNo)
                    .IsRequired()
                    .HasColumnName("Ref_No")
                    .HasMaxLength(50);

                entity.Property(e => e.RefTyp)
                    .IsRequired()
                    .HasColumnName("Ref_Typ")
                    .HasMaxLength(3);

                entity.Property(e => e.Rmks)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.TktAmt)
                    .HasColumnName("Tkt_Amt")
                    .HasColumnType("decimal(9, 2)");

                entity.Property(e => e.TktDate)
                    .HasColumnName("Tkt_Date")
                    .HasColumnType("datetime");

                entity.HasOne(d => d.Dpnd)
                    .WithMany(p => p.HrTrvlTktDtl)
                    .HasForeignKey(d => d.DpndId)
                    .HasConstraintName("FK_HR_TrvlTktDtl_HR_EmpDependent");

                entity.HasOne(d => d.EmpSys)
                    .WithMany(p => p.HrTrvlTktDtl)
                    .HasForeignKey(d => d.EmpSysId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_TrvlTktDtl_HR_EmpGenInfo");
            });

            modelBuilder.Entity<HrWkRoster>(entity =>
            {
                entity.HasKey(e => e.RstId);

                entity.ToTable("HR_WkRoster");

                entity.HasIndex(e => e.RstCode)
                    .HasName("IX_HR_WkRoster_Sft_Code")
                    .IsUnique();

                entity.Property(e => e.RstId).HasColumnName("Rst_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DescArb)
                    .HasColumnName("Desc_Arb")
                    .HasMaxLength(200);

                entity.Property(e => e.DescEng)
                    .HasColumnName("Desc_Eng")
                    .HasMaxLength(200);

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.NameArb)
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(30);

                entity.Property(e => e.NameEng)
                    .IsRequired()
                    .HasColumnName("Name_Eng")
                    .HasMaxLength(30);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RstCode)
                    .IsRequired()
                    .HasColumnName("Rst_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.SftNos)
                    .HasColumnName("Sft_Nos")
                    .HasDefaultValueSql("((1))");

                entity.HasOne(d => d.Ent)
                    .WithMany(p => p.HrWkRoster)
                    .HasForeignKey(d => d.EntId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_WkRoster_ORG_Entity");
            });

            modelBuilder.Entity<HrWkRosterShift>(entity =>
            {
                entity.HasKey(e => e.SchId);

                entity.ToTable("HR_WkRosterShift");

                entity.Property(e => e.SchId).HasColumnName("Sch_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Day)
                    .IsRequired()
                    .HasMaxLength(3);

                entity.Property(e => e.DescArb)
                    .HasColumnName("Desc_Arb")
                    .HasMaxLength(100);

                entity.Property(e => e.DescEng)
                    .IsRequired()
                    .HasColumnName("Desc_Eng")
                    .HasMaxLength(100);

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.PrfIndx)
                    .HasColumnName("Prf_Indx")
                    .HasColumnType("decimal(3, 2)");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RstId).HasColumnName("Rst_Id");

                entity.Property(e => e.SftId).HasColumnName("Sft_Id");

                entity.HasOne(d => d.Ent)
                    .WithMany(p => p.HrWkRosterShift)
                    .HasForeignKey(d => d.EntId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_WkRosterShift_ORG_Entity");

                entity.HasOne(d => d.Rst)
                    .WithMany(p => p.HrWkRosterShift)
                    .HasForeignKey(d => d.RstId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_WkRosterShift_HR_WkRoster");

                entity.HasOne(d => d.Sft)
                    .WithMany(p => p.HrWkRosterShift)
                    .HasForeignKey(d => d.SftId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_WkRosterShift_HR_WkShift");
            });

            modelBuilder.Entity<HrWkShift>(entity =>
            {
                entity.HasKey(e => e.SftId);

                entity.ToTable("HR_WkShift");

                entity.HasIndex(e => e.SftCode)
                    .IsUnique();

                entity.Property(e => e.SftId).HasColumnName("Sft_Id");

                entity.Property(e => e.BrkMnt).HasColumnName("Brk_Mnt");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CutOffTimeEnd)
                    .HasColumnName("CutOff_Time_End")
                    .HasColumnType("datetime");

                entity.Property(e => e.CutOffTimeStart)
                    .HasColumnName("CutOff_Time_Start")
                    .HasColumnType("datetime");

                entity.Property(e => e.Desc).HasMaxLength(200);

                entity.Property(e => e.EOtEndTime).HasColumnName("E_Ot_End_Time");

                entity.Property(e => e.EOtIncld).HasColumnName("E_Ot_Incld");

                entity.Property(e => e.EOtMinMnt).HasColumnName("E_Ot_Min_Mnt");

                entity.Property(e => e.EOtStTime).HasColumnName("E_Ot_St_Time");

                entity.Property(e => e.EndTime)
                    .HasColumnName("End_Time")
                    .HasColumnType("datetime");

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.GrcPrd).HasColumnName("Grc_Prd");

                entity.Property(e => e.NOtEndTime).HasColumnName("N_Ot_End_Time");

                entity.Property(e => e.NOtIncld).HasColumnName("N_Ot_Incld");

                entity.Property(e => e.NOtMinMnt).HasColumnName("N_Ot_Min_Mnt");

                entity.Property(e => e.NOtStTime).HasColumnName("N_Ot_St_Time");

                entity.Property(e => e.NameArb)
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(30);

                entity.Property(e => e.NameEng)
                    .IsRequired()
                    .HasColumnName("Name_Eng")
                    .HasMaxLength(30);

                entity.Property(e => e.OffDay).HasColumnName("Off_Day");

                entity.Property(e => e.OvrNghtSft).HasColumnName("Ovr_Nght_Sft");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.SftCode)
                    .IsRequired()
                    .HasColumnName("Sft_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.StTime)
                    .HasColumnName("St_Time")
                    .HasColumnType("datetime");

                entity.HasOne(d => d.Ent)
                    .WithMany(p => p.HrWkShift)
                    .HasForeignKey(d => d.EntId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_WkShift_ORG_Entity");
            });

            modelBuilder.Entity<LogAtndc>(entity =>
            {
                entity.ToTable("LOG_ATNDC");

                entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

                entity.Property(e => e.AtndcDate)
                    .HasColumnName("Atndc_Date")
                    .HasColumnType("date");

                entity.Property(e => e.CrtBy).HasColumnName("Crt_By");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");

                entity.Property(e => e.NewVal)
                    .IsRequired()
                    .HasColumnName("New_Val")
                    .HasMaxLength(500);

                entity.Property(e => e.OldVal)
                    .HasColumnName("Old_Val")
                    .HasMaxLength(500);

                entity.Property(e => e.Property)
                    .IsRequired()
                    .HasMaxLength(15);

                entity.Property(e => e.RecId).HasColumnName("Rec_Id");

                entity.Property(e => e.SftId).HasColumnName("Sft_Id");

                entity.Property(e => e.TblId).HasColumnName("Tbl_Id");

                entity.Property(e => e.TrType).HasColumnName("Tr_Type");

                entity.HasOne(d => d.CrtByNavigation)
                    .WithMany(p => p.LogAtndc)
                    .HasForeignKey(d => d.CrtBy)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LOG_ATNDC_HR_EmpGenInfo");

                entity.HasOne(d => d.Tbl)
                    .WithMany(p => p.LogAtndc)
                    .HasForeignKey(d => d.TblId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LOG_ATNDC_SYS_TableInfo");
            });

            modelBuilder.Entity<LogAtndcLdPrc>(entity =>
            {
                entity.HasKey(e => e.LogId)
                    .HasName("PK_LOG_LOG_ATNDC_LD_PRC");

                entity.ToTable("LOG_ATNDC_LD_PRC");

                entity.Property(e => e.LogId).HasColumnName("Log_Id");

                entity.Property(e => e.FrmDate)
                    .HasColumnName("Frm_Date")
                    .HasColumnType("date");

                entity.Property(e => e.PrcBy).HasColumnName("Prc_By");

                entity.Property(e => e.PrcDateTime)
                    .HasColumnName("Prc_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.PrcSrc)
                    .IsRequired()
                    .HasColumnName("Prc_Src")
                    .HasMaxLength(10);

                entity.Property(e => e.PrcSrcDtl)
                    .IsRequired()
                    .HasColumnName("Prc_Src_Dtl")
                    .HasMaxLength(50);

                entity.Property(e => e.PrcTyp)
                    .IsRequired()
                    .HasColumnName("Prc_Typ")
                    .HasMaxLength(1);

                entity.Property(e => e.RefId).HasColumnName("Ref_Id");

                entity.Property(e => e.ToDate)
                    .HasColumnName("To_Date")
                    .HasColumnType("date");
            });

            modelBuilder.Entity<LogHremp>(entity =>
            {
                entity.ToTable("LOG_HREMP");

                entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

                entity.Property(e => e.CrtBy).HasColumnName("Crt_By");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.NewVal)
                    .IsRequired()
                    .HasColumnName("New_Val")
                    .HasMaxLength(500);

                entity.Property(e => e.OldVal)
                    .HasColumnName("Old_Val")
                    .HasMaxLength(500);

                entity.Property(e => e.Property)
                    .IsRequired()
                    .HasMaxLength(15);

                entity.Property(e => e.RecId).HasColumnName("Rec_Id");

                entity.Property(e => e.TblId).HasColumnName("Tbl_Id");

                entity.Property(e => e.TrType).HasColumnName("Tr_Type");

                entity.HasOne(d => d.CrtByNavigation)
                    .WithMany(p => p.LogHremp)
                    .HasForeignKey(d => d.CrtBy)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LOG_HREMP_HR_EmpGenInfo");

                entity.HasOne(d => d.Tbl)
                    .WithMany(p => p.LogHremp)
                    .HasForeignKey(d => d.TblId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LOG_HREMP_SYS_TableInfo");
            });

            modelBuilder.Entity<LogHrlv>(entity =>
            {
                entity.ToTable("LOG_HRLV");

                entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

                entity.Property(e => e.CrtBy).HasColumnName("Crt_By");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.NewVal)
                    .IsRequired()
                    .HasColumnName("New_Val")
                    .HasMaxLength(500);

                entity.Property(e => e.OldVal)
                    .HasColumnName("Old_Val")
                    .HasMaxLength(500);

                entity.Property(e => e.Property)
                    .IsRequired()
                    .HasMaxLength(15);

                entity.Property(e => e.RecId).HasColumnName("Rec_Id");

                entity.Property(e => e.TblId).HasColumnName("Tbl_Id");

                entity.Property(e => e.TrType).HasColumnName("Tr_Type");

                entity.HasOne(d => d.CrtByNavigation)
                    .WithMany(p => p.LogHrlv)
                    .HasForeignKey(d => d.CrtBy)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LOG_HRLV_HR_EmpGenInfo");

                entity.HasOne(d => d.Tbl)
                    .WithMany(p => p.LogHrlv)
                    .HasForeignKey(d => d.TblId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LOG_HRLV_SYS_TableInfo");
            });

            modelBuilder.Entity<LogHrpr>(entity =>
            {
                entity.ToTable("LOG_HRPR");

                entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

                entity.Property(e => e.CrtBy).HasColumnName("Crt_By");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.NewVal)
                    .IsRequired()
                    .HasColumnName("New_Val")
                    .HasMaxLength(500);

                entity.Property(e => e.OldVal)
                    .HasColumnName("Old_Val")
                    .HasMaxLength(500);

                entity.Property(e => e.Property)
                    .IsRequired()
                    .HasMaxLength(15);

                entity.Property(e => e.RecId).HasColumnName("Rec_Id");

                entity.Property(e => e.TblId).HasColumnName("Tbl_Id");

                entity.Property(e => e.TrType).HasColumnName("Tr_Type");

                entity.HasOne(d => d.CrtByNavigation)
                    .WithMany(p => p.LogHrpr)
                    .HasForeignKey(d => d.CrtBy)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LOG_HRPR_HR_EmpGenInfo");

                entity.HasOne(d => d.Tbl)
                    .WithMany(p => p.LogHrpr)
                    .HasForeignKey(d => d.TblId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LOG_HRPR_SYS_TableInfo");
            });

            modelBuilder.Entity<LogHrwsf>(entity =>
            {
                entity.ToTable("LOG_HRWSF");

                entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

                entity.Property(e => e.CrtBy).HasColumnName("Crt_By");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.NewVal)
                    .IsRequired()
                    .HasColumnName("New_Val")
                    .HasMaxLength(500);

                entity.Property(e => e.OldVal)
                    .HasColumnName("Old_Val")
                    .HasMaxLength(500);

                entity.Property(e => e.Property)
                    .IsRequired()
                    .HasMaxLength(15);

                entity.Property(e => e.RecId).HasColumnName("Rec_Id");

                entity.Property(e => e.TblId).HasColumnName("Tbl_Id");

                entity.Property(e => e.TrType).HasColumnName("Tr_Type");

                entity.HasOne(d => d.CrtByNavigation)
                    .WithMany(p => p.LogHrwsf)
                    .HasForeignKey(d => d.CrtBy)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LOG_HRWSF_HR_EmpGenInfo");

                entity.HasOne(d => d.Tbl)
                    .WithMany(p => p.LogHrwsf)
                    .HasForeignKey(d => d.TblId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LOG_HRWSF_SYS_TableInfo");
            });

            modelBuilder.Entity<LogLvLdPrc>(entity =>
            {
                entity.HasKey(e => e.LogId);

                entity.ToTable("LOG_LV_LD_PRC");

                entity.Property(e => e.LogId).HasColumnName("Log_Id");

                entity.Property(e => e.FrmIncdnt).HasColumnName("Frm_Incdnt");

                entity.Property(e => e.PrcBy).HasColumnName("Prc_By");

                entity.Property(e => e.PrcDateTime)
                    .HasColumnName("Prc_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.PrcEnd)
                    .HasColumnName("Prc_End")
                    .HasColumnType("datetime");

                entity.Property(e => e.PrcSrc)
                    .IsRequired()
                    .HasColumnName("Prc_Src")
                    .HasMaxLength(10);

                entity.Property(e => e.PrcSrcDtl)
                    .IsRequired()
                    .HasColumnName("Prc_Src_Dtl")
                    .HasMaxLength(50);

                entity.Property(e => e.PrcSt)
                    .HasColumnName("Prc_St")
                    .HasColumnType("datetime");

                entity.Property(e => e.PrcStat)
                    .IsRequired()
                    .HasColumnName("Prc_Stat")
                    .HasMaxLength(10);

                entity.Property(e => e.PrcTyp)
                    .IsRequired()
                    .HasColumnName("Prc_Typ")
                    .HasMaxLength(1);

                entity.Property(e => e.RstAll).HasColumnName("Rst_All");

                entity.Property(e => e.ToIncdnt).HasColumnName("To_Incdnt");
            });

            modelBuilder.Entity<LogOrg>(entity =>
            {
                entity.ToTable("LOG_ORG");

                entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

                entity.Property(e => e.CrtBy).HasColumnName("Crt_By");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.NewVal)
                    .IsRequired()
                    .HasColumnName("New_Val")
                    .HasMaxLength(500);

                entity.Property(e => e.OldVal)
                    .HasColumnName("Old_Val")
                    .HasMaxLength(500);

                entity.Property(e => e.Property)
                    .IsRequired()
                    .HasMaxLength(15);

                entity.Property(e => e.RecId).HasColumnName("Rec_Id");

                entity.Property(e => e.TblId).HasColumnName("Tbl_Id");

                entity.Property(e => e.TrType).HasColumnName("Tr_Type");

                entity.HasOne(d => d.CrtByNavigation)
                    .WithMany(p => p.LogOrg)
                    .HasForeignKey(d => d.CrtBy)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LOG_ORG_HR_EmpGenInfo");

                entity.HasOne(d => d.Tbl)
                    .WithMany(p => p.LogOrg)
                    .HasForeignKey(d => d.TblId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LOG_ORG_SYS_TableInfo");
            });

            modelBuilder.Entity<OrgCountry>(entity =>
            {
                entity.HasKey(e => e.CntryId);

                entity.ToTable("ORG_Country");

                entity.HasIndex(e => e.CntryNameEng)
                    .HasName("IX_ORG_Country_Name")
                    .IsUnique();

                entity.Property(e => e.CntryId).HasColumnName("Cntry_Id");

                entity.Property(e => e.CntryNameArb)
                    .HasColumnName("Cntry_Name_Arb")
                    .HasMaxLength(20);

                entity.Property(e => e.CntryNameEng)
                    .IsRequired()
                    .HasColumnName("Cntry_Name_Eng")
                    .HasMaxLength(20);

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.Note).HasMaxLength(100);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");
            });

            modelBuilder.Entity<OrgDepartment>(entity =>
            {
                entity.HasKey(e => e.DeptId);

                entity.ToTable("ORG_Department");

                entity.HasIndex(e => e.DeptCode)
                    .IsUnique();

                entity.Property(e => e.DeptId).HasColumnName("Dept_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DeptCode)
                    .IsRequired()
                    .HasColumnName("Dept_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.DescArb)
                    .HasColumnName("Desc_Arb")
                    .HasMaxLength(200);

                entity.Property(e => e.DescEng)
                    .HasColumnName("Desc_Eng")
                    .HasMaxLength(200);

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.NameArb)
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(50);

                entity.Property(e => e.NameEng)
                    .IsRequired()
                    .HasColumnName("Name_Eng")
                    .HasMaxLength(50);

                entity.Property(e => e.ParentId).HasColumnName("Parent_Id");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.ShtName)
                    .HasColumnName("Sht_Name")
                    .HasMaxLength(15);

                entity.Property(e => e.SrtOrd)
                    .HasColumnName("Srt_Ord")
                    .HasDefaultValueSql("((100))");

                entity.HasOne(d => d.Ent)
                    .WithMany(p => p.OrgDepartment)
                    .HasForeignKey(d => d.EntId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ORG_Department_ORG_Entity");
            });

            modelBuilder.Entity<OrgDesignation>(entity =>
            {
                entity.HasKey(e => e.DesigId);

                entity.ToTable("ORG_Designation");

                entity.HasIndex(e => e.DesigCode)
                    .HasName("IX_ORG_Department_Desig_Code")
                    .IsUnique();

                entity.Property(e => e.DesigId).HasColumnName("Desig_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DesigCode)
                    .IsRequired()
                    .HasColumnName("Desig_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.DesigLevel).HasColumnName("Desig_Level");

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.JobDescArb)
                    .HasColumnName("Job_Desc_Arb")
                    .HasMaxLength(300);

                entity.Property(e => e.JobDescEng)
                    .HasColumnName("Job_Desc_Eng")
                    .HasMaxLength(300);

                entity.Property(e => e.MaxEmpGrd)
                    .HasColumnName("Max_Emp_Grd")
                    .HasDefaultValueSql("((100))");

                entity.Property(e => e.MinEmpGrd).HasColumnName("Min_Emp_Grd");

                entity.Property(e => e.NameArb)
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(50);

                entity.Property(e => e.NameEng)
                    .IsRequired()
                    .HasColumnName("Name_Eng")
                    .HasMaxLength(50);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.HasOne(d => d.Ent)
                    .WithMany(p => p.OrgDesignation)
                    .HasForeignKey(d => d.EntId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ORG_Designation_ORG_Entity");
            });

            modelBuilder.Entity<OrgEmpDocType>(entity =>
            {
                entity.HasKey(e => e.DocTypId);

                entity.ToTable("ORG_EmpDocType");

                entity.HasIndex(e => e.DocTypCode)
                    .IsUnique();

                entity.Property(e => e.DocTypId).HasColumnName("Doc_Typ_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DescArb)
                    .HasColumnName("Desc_Arb")
                    .HasMaxLength(100);

                entity.Property(e => e.DescEng)
                    .HasColumnName("Desc_Eng")
                    .HasMaxLength(100);

                entity.Property(e => e.DocTypCode)
                    .IsRequired()
                    .HasColumnName("Doc_Typ_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.IsMandatory).HasColumnName("Is_Mandatory");

                entity.Property(e => e.NameArb)
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(30);

                entity.Property(e => e.NameEng)
                    .IsRequired()
                    .HasColumnName("Name_Eng")
                    .HasMaxLength(30);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.HasOne(d => d.Ent)
                    .WithMany(p => p.OrgEmpDocType)
                    .HasForeignKey(d => d.EntId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ORG_EmpDocType_ORG_Entity");
            });

            modelBuilder.Entity<OrgEntity>(entity =>
            {
                entity.HasKey(e => e.EntId);

                entity.ToTable("ORG_Entity");

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Desc).HasMaxLength(200);

                entity.Property(e => e.EntCode)
                    .IsRequired()
                    .HasColumnName("Ent_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.NameArb)
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(50);

                entity.Property(e => e.NameEng)
                    .IsRequired()
                    .HasColumnName("Name_Eng")
                    .HasMaxLength(50);

                entity.Property(e => e.OrgId).HasColumnName("Org_Id");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.HasOne(d => d.Org)
                    .WithMany(p => p.OrgEntity)
                    .HasForeignKey(d => d.OrgId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ORG_Entity_ORG_Info");
            });

            modelBuilder.Entity<OrgHoliday>(entity =>
            {
                entity.HasKey(e => e.HdId);

                entity.ToTable("ORG_Holiday");

                entity.Property(e => e.HdId).HasColumnName("Hd_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.EndDate)
                    .HasColumnName("End_Date")
                    .HasColumnType("date");

                entity.Property(e => e.NameArb)
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(50);

                entity.Property(e => e.NameEng)
                    .IsRequired()
                    .HasColumnName("Name_Eng")
                    .HasMaxLength(50);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.StDate)
                    .HasColumnName("St_Date")
                    .HasColumnType("date");
            });

            modelBuilder.Entity<OrgInfo>(entity =>
            {
                entity.HasKey(e => e.OrgId);

                entity.ToTable("ORG_Info");

                entity.Property(e => e.OrgId).HasColumnName("Org_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Desc).HasMaxLength(200);

                entity.Property(e => e.NameArb)
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(50);

                entity.Property(e => e.NameEng)
                    .IsRequired()
                    .HasColumnName("Name_Eng")
                    .HasMaxLength(50);

                entity.Property(e => e.OrgCode)
                    .IsRequired()
                    .HasColumnName("Org_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.OrgLogo1).HasColumnName("Org_Logo1");

                entity.Property(e => e.OrgLogo2).HasColumnName("Org_Logo2");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");
            });

            modelBuilder.Entity<OrgLocation>(entity =>
            {
                entity.HasKey(e => e.LocId);

                entity.ToTable("ORG_Location");

                entity.HasIndex(e => e.LocCode)
                    .IsUnique();

                entity.Property(e => e.LocId).HasColumnName("Loc_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DescArb)
                    .HasColumnName("Desc_Arb")
                    .HasMaxLength(100);

                entity.Property(e => e.DescEng)
                    .HasColumnName("Desc_Eng")
                    .HasMaxLength(100);

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.LocCode)
                    .IsRequired()
                    .HasColumnName("Loc_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.NameArb)
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(30);

                entity.Property(e => e.NameEng)
                    .IsRequired()
                    .HasColumnName("Name_Eng")
                    .HasMaxLength(30);

                entity.Property(e => e.ParentId).HasColumnName("Parent_Id");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.HasOne(d => d.Ent)
                    .WithMany(p => p.OrgLocation)
                    .HasForeignKey(d => d.EntId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ORG_Location_ORG_Entity");
            });

            modelBuilder.Entity<OrgNationality>(entity =>
            {
                entity.HasKey(e => e.NltyId);

                entity.ToTable("ORG_Nationality");

                entity.HasIndex(e => e.NltyId)
                    .HasName("IX_ORG_Nationality_Name")
                    .IsUnique();

                entity.Property(e => e.NltyId).HasColumnName("Nlty_Id");

                entity.Property(e => e.CntryId).HasColumnName("Cntry_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.NltyNameArb)
                    .HasColumnName("Nlty_Name_Arb")
                    .HasMaxLength(20);

                entity.Property(e => e.NltyNameEng)
                    .IsRequired()
                    .HasColumnName("Nlty_Name_Eng")
                    .HasMaxLength(20);

                entity.Property(e => e.NltyTypId).HasColumnName("NltyTyp_Id");

                entity.Property(e => e.Note).HasMaxLength(100);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.HasOne(d => d.Cntry)
                    .WithMany(p => p.OrgNationality)
                    .HasForeignKey(d => d.CntryId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ORG_Nationality_ORG_Country");

                entity.HasOne(d => d.NltyTyp)
                    .WithMany(p => p.OrgNationality)
                    .HasForeignKey(d => d.NltyTypId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ORG_Nationality_ORG_NltyType");
            });

            modelBuilder.Entity<OrgNltyEmpRatio>(entity =>
            {
                entity.HasKey(e => e.EmpRtoId);

                entity.ToTable("ORG_NltyEmpRatio");

                entity.Property(e => e.EmpRtoId).HasColumnName("EmpRto_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.NltyId).HasColumnName("Nlty_Id");

                entity.Property(e => e.NltyTypId).HasColumnName("NltyTyp_Id");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.StfRto)
                    .HasColumnName("Stf_Rto")
                    .HasColumnType("decimal(5, 2)");

                entity.HasOne(d => d.Ent)
                    .WithMany(p => p.OrgNltyEmpRatio)
                    .HasForeignKey(d => d.EntId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ORG_NltyEmpRatio_ORG_Entity");

                entity.HasOne(d => d.Nlty)
                    .WithMany(p => p.OrgNltyEmpRatio)
                    .HasForeignKey(d => d.NltyId)
                    .HasConstraintName("FK_ORG_NltyEmpRatio_ORG_Nationality");

                entity.HasOne(d => d.NltyTyp)
                    .WithMany(p => p.OrgNltyEmpRatio)
                    .HasForeignKey(d => d.NltyTypId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ORG_NltyEmpRatio_ORG_NltyType");
            });

            modelBuilder.Entity<OrgNltyType>(entity =>
            {
                entity.HasKey(e => e.NltyTypId);

                entity.ToTable("ORG_NltyType");

                entity.HasIndex(e => e.NltyTypId)
                    .HasName("IX_NltyTyp_Name_Eng")
                    .IsUnique();

                entity.Property(e => e.NltyTypId).HasColumnName("NltyTyp_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.NltyTypNameArb)
                    .HasColumnName("NltyTyp_Name_Arb")
                    .HasMaxLength(20);

                entity.Property(e => e.NltyTypNameEng)
                    .IsRequired()
                    .HasColumnName("NltyTyp_Name_Eng")
                    .HasMaxLength(20);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");
            });

            modelBuilder.Entity<OrgWrkType>(entity =>
            {
                entity.HasKey(e => e.WrktypId);

                entity.ToTable("ORG_WrkType");

                entity.Property(e => e.WrktypId).HasColumnName("Wrktyp_Id");

                entity.Property(e => e.WrktypCode)
                    .IsRequired()
                    .HasColumnName("Wrktyp_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.NameEng)
                    .IsRequired()
                    .HasColumnName("Name_Eng")
                    .HasMaxLength(50);

                entity.Property(e => e.NameArb)
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(50);

                entity.Property(e => e.Desc)
                    .HasMaxLength(100);

                entity.Property(e => e.EntId)
                    .HasColumnName("Ent_Id")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.CrtId)
                    .HasColumnName("Crt_Id")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<ReCnfgRpt>(entity =>
            {
                entity.HasKey(e => e.RptId);

                entity.ToTable("RE_Cnfg_Rpt");

                entity.Property(e => e.RptId)
                    .ValueGeneratedNever()
                    .HasColumnName("Rpt_Id");

                entity.Property(e => e.CrtDt)
                    .HasColumnType("datetime")
                    .HasColumnName("Crt_DT")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.EntId).HasColumnName("Ent_Id");

                entity.Property(e => e.PrntId).HasColumnName("Prnt_Id");

                entity.Property(e => e.RcSts)
                    .HasColumnName("Rc_Sts")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RptDsplyNm)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("Rpt_Dsply_Nm");

                entity.Property(e => e.RptFlNm)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("Rpt_Fl_Nm");

                entity.Property(e => e.RptGrpId).HasColumnName("Rpt_Grp_Id");

                entity.Property(e => e.RptKy)
                    .IsRequired()
                    .HasMaxLength(30)
                    .HasColumnName("Rpt_Ky");

                entity.Property(e => e.SbIndx).HasColumnName("Sb_Indx");

                entity.Property(e => e.SpNm)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("Sp_Nm");

                entity.HasOne(d => d.Prnt)
                    .WithMany(p => p.InversePrnt)
                    .HasForeignKey(d => d.PrntId)
                    .HasConstraintName("FK_RE_Cnfg_Rpt_RE_Cnfg_Rpt");
            });

            modelBuilder.Entity<ReCnfgRptPrm>(entity =>
            {
                entity.HasKey(e => e.PrmId);

                entity.ToTable("RE_Cnfg_RptPrm");

                entity.Property(e => e.PrmId)
                    .ValueGeneratedNever()
                    .HasColumnName("Prm_Id");

                entity.Property(e => e.CrtDt)
                    .HasColumnType("datetime")
                    .HasColumnName("Crt_DT")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.PrmNm)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnName("Prm_Nm");

                entity.Property(e => e.PrmTyp)
                    .IsRequired()
                    .HasMaxLength(30)
                    .HasColumnName("Prm_Typ");

                entity.Property(e => e.RcSts)
                    .HasColumnName("Rc_Sts")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RptId).HasColumnName("Rpt_Id");

                entity.Property(e => e.Rqrd)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.SpPrm)
                    .IsRequired()
                    .HasColumnName("Sp_Prm")
                    .HasDefaultValueSql("((1))");

                entity.HasOne(d => d.Rpt)
                    .WithMany(p => p.ReCnfgRptPrms)
                    .HasForeignKey(d => d.RptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_RE_Cnfg_RptPrm_RE_Cnfg_Rpt");
            });

            modelBuilder.Entity<ReRptLog>(entity =>
            {
                entity.HasKey(e => e.RptLogId);

                entity.ToTable("RE_Rpt_Log");

                entity.Property(e => e.RptLogId).HasColumnName("RptLog_Id");

                entity.Property(e => e.Prms).IsRequired();

                entity.Property(e => e.PrntdDt)
                    .HasColumnType("datetime")
                    .HasColumnName("Prntd_DT")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.PrntdId).HasColumnName("Prntd_Id");

                entity.Property(e => e.RcSts)
                    .HasColumnName("Rc_Sts")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RptId).HasColumnName("Rpt_Id");

                entity.Property(e => e.SsnId).HasColumnName("Ssn_Id");

                entity.HasOne(d => d.Rpt)
                    .WithMany(p => p.ReRptLogs)
                    .HasForeignKey(d => d.RptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_RE_Rpt_Log_RE_Cnfg_Rpt");
            });

            modelBuilder.Entity<ReStgngRpt>(entity =>
            {
                entity.HasKey(e => e.SsnRptId);

                entity.ToTable("RE_Stgng_Rpt");

                entity.Property(e => e.SsnRptId).HasColumnName("Ssn_Rpt_Id");

                entity.Property(e => e.RptId).HasColumnName("Rpt_Id");

                entity.Property(e => e.SsnId).HasColumnName("Ssn_Id");

                entity.HasOne(d => d.Rpt)
                    .WithMany(p => p.ReStgngRpts)
                    .HasForeignKey(d => d.RptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_RE_Stgng_Rpt_RE_Cnfg_Rpt");

                entity.HasOne(d => d.Ssn)
                    .WithMany(p => p.ReStgngRpts)
                    .HasForeignKey(d => d.SsnId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_RE_Stgng_Rpt_RE_Stgng_Ssn");
            });

            modelBuilder.Entity<ReStgngRptPrm>(entity =>
            {
                entity.HasKey(e => new { e.SsnRptId, e.PrmId });

                entity.ToTable("RE_Stgng_RptPrm");

                entity.Property(e => e.SsnRptId).HasColumnName("Ssn_Rpt_Id");

                entity.Property(e => e.PrmId).HasColumnName("Prm_Id");

                entity.Property(e => e.PrmVlv)
                    .IsRequired()
                    .HasColumnName("Prm_Vlv");

                entity.HasOne(d => d.Prm)
                    .WithMany(p => p.ReStgngRptPrms)
                    .HasForeignKey(d => d.PrmId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_RE_Stgng_RptPrm_RE_Cnfg_RptPrm");

                entity.HasOne(d => d.SsnRpt)
                    .WithMany(p => p.ReStgngRptPrms)
                    .HasForeignKey(d => d.SsnRptId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_RE_Stgng_RptPrm_RE_Stgng_Rpt");
            });

            modelBuilder.Entity<ReStgngSsn>(entity =>
            {
                entity.HasKey(e => e.SsnId);

                entity.ToTable("RE_Stgng_Ssn");

                entity.Property(e => e.SsnId).HasColumnName("Ssn_Id");

                entity.Property(e => e.Exprt)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.Prnt)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.PrntdDt)
                    .HasColumnType("datetime")
                    .HasColumnName("Prntd_DT")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.PrntdId).HasColumnName("Prntd_Id");

                entity.Property(e => e.RcSts)
                    .HasColumnName("Rc_Sts")
                    .HasDefaultValueSql("((1))");
            });

            modelBuilder.Entity<SysDshbrd>(entity =>
            {
                entity.HasKey(e => e.DshbrdId);

                entity.ToTable("SYS_Dshbrd");

                entity.Property(e => e.DshbrdId).HasColumnName("Dshbrd_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Desc).HasMaxLength(50);

                entity.Property(e => e.DshbrdIndxTxt)
                    .IsRequired()
                    .HasColumnName("Dshbrd_Indx_Txt")
                    .HasMaxLength(20);

                entity.Property(e => e.DshbrdName)
                    .HasColumnName("Dshbrd_Name")
                    .HasMaxLength(50);

                entity.Property(e => e.EnbldOpt1).HasColumnName("Enbld_Opt_1");

                entity.Property(e => e.EnbldOpt2).HasColumnName("Enbld_Opt_2");

                entity.Property(e => e.FlAccss).HasColumnName("Fl_Accss");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");
            });

            modelBuilder.Entity<SysAtndcStat>(entity =>
            {
                entity.HasKey(e => e.AtndcStatId);

                entity.ToTable("SYS_AtndcStat");

                entity.Property(e => e.AtndcStatId)
                    .HasColumnName("Atndc_Stat_Id")
                    .ValueGeneratedNever();

                entity.Property(e => e.AtndcStatTxt)
                    .IsRequired()
                    .HasColumnName("Atndc_Stat_Txt")
                    .HasMaxLength(25);

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Desc).HasMaxLength(50);

                entity.Property(e => e.PrntId).HasColumnName("Prnt_Id");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");
            });

            modelBuilder.Entity<SysConfig>(entity =>
            {
                entity.ToTable("SYS_Config");

                entity.Property(e => e.FromEmail)
                    .HasColumnName("From_Email")
                    .HasMaxLength(50);

                entity.Property(e => e.Server).HasMaxLength(50);

                entity.Property(e => e.ServiceAcct)
                    .HasColumnName("Service_Acct")
                    .HasMaxLength(50);

                entity.Property(e => e.ServiceAcctPwd)
                    .HasColumnName("Service_AcctPwd")
                    .HasMaxLength(50);

                entity.Property(e => e.TestEmail)
                    .HasColumnName("Test_Email")
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<SysDshbrd>(entity =>
            {
                entity.HasKey(e => e.DshbrdId);

                entity.ToTable("SYS_Dshbrd");

                entity.Property(e => e.DshbrdId).HasColumnName("Dshbrd_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Desc).HasMaxLength(50);

                entity.Property(e => e.DshbrdIndxTxt)
                    .IsRequired()
                    .HasColumnName("Dshbrd_Indx_Txt")
                    .HasMaxLength(20);

                entity.Property(e => e.DshbrdName)
                    .HasColumnName("Dshbrd_Name")
                    .HasMaxLength(50);

                entity.Property(e => e.EnbldOpt1).HasColumnName("Enbld_Opt_1");

                entity.Property(e => e.EnbldOpt2).HasColumnName("Enbld_Opt_2");

                entity.Property(e => e.FlAccss).HasColumnName("Fl_Accss");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");
            });

            modelBuilder.Entity<SysError>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SYS_Error");

                entity.Property(e => e.ErrCode)
                    .IsRequired()
                    .HasColumnName("Err_code")
                    .HasMaxLength(50);

                entity.Property(e => e.ErrName)
                    .IsRequired()
                    .HasColumnName("Err_name")
                    .HasMaxLength(500);

                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.ModDateTime)
                    .HasColumnName("Mod_DateTime")
                    .HasColumnType("datetime");
            });

            modelBuilder.Entity<SysSelectListElement>(entity =>
            {
                entity.HasKey(e => e.LstElmntId);

                entity.ToTable("SYS_SelectListElement");

                entity.Property(e => e.LstElmntId).HasColumnName("Lst_Elmnt_Id");

                entity.Property(e => e.CrtDatetime)
                    .HasColumnName("Crt_Datetime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Desc).HasMaxLength(50);

                entity.Property(e => e.ElmntTxt)
                    .IsRequired()
                    .HasColumnName("Elmnt_Txt")
                    .HasMaxLength(30);

                entity.Property(e => e.LstTypId).HasColumnName("Lst_Typ_Id");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.ResTyp)
                    .HasColumnName("Res_Typ")
                    .HasDefaultValueSql("((1))");

                entity.HasOne(d => d.LstTyp)
                    .WithMany(p => p.SysSelectListElement)
                    .HasForeignKey(d => d.LstTypId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_SYS_SelectListElement_SYS_SelectListType");
            });

            modelBuilder.Entity<SysSelectListType>(entity =>
            {
                entity.HasKey(e => e.LstTypId);

                entity.ToTable("SYS_SelectListType");

                entity.HasIndex(e => e.LstTypKey)
                    .IsUnique();

                entity.Property(e => e.LstTypId).HasColumnName("Lst_Typ_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Desc).HasMaxLength(150);

                entity.Property(e => e.LstTypKey)
                    .IsRequired()
                    .HasColumnName("Lst_Typ_Key")
                    .HasMaxLength(12);

                entity.Property(e => e.LstTypName)
                    .IsRequired()
                    .HasColumnName("Lst_Typ_Name")
                    .HasMaxLength(50);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.Section)
                    .IsRequired()
                    .HasMaxLength(8);
            });

            modelBuilder.Entity<SysSystemPrivilege>(entity =>
            {
                entity.HasKey(e => e.PrvId);

                entity.ToTable("SYS_SystemPrivilege");

                entity.Property(e => e.PrvId)
                    .HasColumnName("Prv_Id")
                    .ValueGeneratedNever();

                entity.Property(e => e.Action).HasMaxLength(20);

                entity.Property(e => e.Area)
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Controller).HasMaxLength(100);

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Icon).HasMaxLength(30);

                entity.Property(e => e.NameArb)
                    .IsRequired()
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(50);

                entity.Property(e => e.NameEng)
                    .IsRequired()
                    .HasColumnName("Name_Eng")
                    .HasMaxLength(50);

                entity.Property(e => e.ParentId)
                    .HasColumnName("Parent_Id")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.PrvCode)
                    .IsRequired()
                    .HasColumnName("Prv_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.RecStat).HasColumnName("Rec_Stat");

                entity.Property(e => e.Target).HasMaxLength(10);

                entity.HasOne(d => d.Parent)
                    .WithMany(p => p.InverseParent)
                    .HasForeignKey(d => d.ParentId)
                    .HasConstraintName("FK_SYS_SystemPrivilege_Parent");
            });

            modelBuilder.Entity<SysTableInfo>(entity =>
            {
                entity.HasKey(e => e.TblId);

                entity.ToTable("SYS_TableInfo");

                entity.HasIndex(e => e.TblName)
                    .IsUnique();

                entity.Property(e => e.TblId).HasColumnName("Tbl_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.MdlName)
                    .IsRequired()
                    .HasColumnName("Mdl_Name")
                    .HasMaxLength(30);

                entity.Property(e => e.Section)
                    .IsRequired()
                    .HasMaxLength(5);

                entity.Property(e => e.TblName)
                    .IsRequired()
                    .HasColumnName("Tbl_Name")
                    .HasMaxLength(30);
            });

            modelBuilder.Entity<TmpLvTypMpng>(entity =>
            {
                entity.HasKey(e => e.AutoId);

                entity.ToTable("TMP_LV_Typ_Mpng");

                entity.Property(e => e.AutoId).HasColumnName("Auto_Id");

                entity.Property(e => e.LvTypId).HasColumnName("LvTyp_Id");

                entity.Property(e => e.Lvtype)
                    .HasColumnName("LVType")
                    .HasMaxLength(50)
                    .IsFixedLength();

                entity.Property(e => e.LvtypeSub)
                    .HasColumnName("LVType_Sub")
                    .HasMaxLength(50)
                    .IsFixedLength();

                entity.Property(e => e.LvtypeSubPay)
                    .HasColumnName("LVType_Sub_Pay")
                    .HasMaxLength(10)
                    .IsFixedLength();
            }); ;

            modelBuilder.Entity<OrgReligion>(entity =>
            {
                entity.HasKey(e => e.RelId);

                entity.ToTable("ORG_Religion");

                entity.Property(e => e.RelId).HasColumnName("Rel_Id");

                entity.Property(e => e.RelNameEng)
                    .IsRequired()
                    .HasColumnName("Rel_Name_Eng")
                    .HasMaxLength(50);

                entity.Property(e => e.RelNameArb)
                    .HasColumnName("Rel_Name_Arb")
                    .HasMaxLength(50);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<WfPolicyType>(entity =>
            {
                entity.HasKey(e => e.WfPlcTypId);

                entity.ToTable("WF_PolicyType");

                entity.HasIndex(e => e.WfPlcTypCode)
                    .IsUnique();

                entity.Property(e => e.WfPlcTypId).HasColumnName("WfPlcTyp_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Desc).HasMaxLength(50);

                entity.Property(e => e.NameArb)
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(20);

                entity.Property(e => e.NameEng)
                    .IsRequired()
                    .HasColumnName("Name_Eng")
                    .HasMaxLength(20);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.WfPlcTypCode)
                    .IsRequired()
                    .HasColumnName("WfPlcTyp_Code")
                    .HasMaxLength(10);
            });

            modelBuilder.Entity<WfStage>(entity =>
            {
                entity.HasKey(e => e.WfStgId);

                entity.ToTable("WF_Stage");

                entity.HasIndex(e => e.WfStgCode)
                    .IsUnique();

                entity.Property(e => e.WfStgId).HasColumnName("WfStg_Id");

                entity.Property(e => e.Color).HasMaxLength(10);

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.GenIns)
                    .HasColumnName("Gen_Ins")
                    .HasMaxLength(200);

                entity.Property(e => e.NameArb)
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(20);

                entity.Property(e => e.NameEng)
                    .IsRequired()
                    .HasColumnName("Name_Eng")
                    .HasMaxLength(20);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.WfStgCode)
                    .IsRequired()
                    .HasColumnName("WfStg_Code")
                    .HasMaxLength(10);
            });

            modelBuilder.Entity<WfStageStatus>(entity =>
            {
                entity.HasKey(e => new { e.WfStgId, e.WfStatId });

                entity.ToTable("WF_StageStatus");

                entity.Property(e => e.WfStgId).HasColumnName("WfStg_Id");

                entity.Property(e => e.WfStatId).HasColumnName("WfStat_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.GenIns)
                    .HasColumnName("Gen_Ins")
                    .HasMaxLength(200);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.HasOne(d => d.WfStat)
                    .WithMany(p => p.WfStageStatus)
                    .HasForeignKey(d => d.WfStatId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_WF_StageStatus_WF_Status");

                entity.HasOne(d => d.WfStg)
                    .WithMany(p => p.WfStageStatus)
                    .HasForeignKey(d => d.WfStgId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_WF_StageStatus_WF_Stage");
            });

            modelBuilder.Entity<WfStatus>(entity =>
            {
                entity.HasKey(e => e.WfStatId);

                entity.ToTable("WF_Status");

                entity.HasIndex(e => e.WfStatCode)
                    .IsUnique();

                entity.Property(e => e.WfStatId).HasColumnName("WfStat_Id");

                entity.Property(e => e.Color).HasMaxLength(10);

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Desc).HasMaxLength(200);

                entity.Property(e => e.NameArb)
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(20);

                entity.Property(e => e.NameEng)
                    .IsRequired()
                    .HasColumnName("Name_Eng")
                    .HasMaxLength(20);

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RsltType).HasColumnName("Rslt_Type");

                entity.Property(e => e.WfStatCode)
                    .IsRequired()
                    .HasColumnName("WfStat_Code")
                    .HasMaxLength(10);
            });

            modelBuilder.Entity<WfStrucAlert>(entity =>
            {
                entity.HasKey(e => e.StrcAltId);

                entity.ToTable("WF_StrucAlert");

                entity.Property(e => e.StrcAltId)
                    .HasColumnName("StrcAlt_Id")
                    .ValueGeneratedNever();

                entity.Property(e => e.AltType).HasColumnName("Alt_Type");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Email).HasMaxLength(50);

                entity.Property(e => e.EmpId).HasColumnName("Emp_Id");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.WfStrcId).HasColumnName("WfStrc_Id");

                entity.HasOne(d => d.WfStrc)
                    .WithMany(p => p.WfStrucAlert)
                    .HasForeignKey(d => d.WfStrcId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_WF_StrucAlert_WF_Structure");
            });

            modelBuilder.Entity<WfStrucStage>(entity =>
            {
                entity.HasKey(e => e.StgId);

                entity.ToTable("WF_StrucStage");

                entity.Property(e => e.StgId).HasColumnName("Stg_Id");

                entity.Property(e => e.ActionAlt).HasColumnName("Action_Alt");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DesigId).HasColumnName("Desig_Id");

                entity.Property(e => e.EmpId).HasColumnName("Emp_Id");

                entity.Property(e => e.Ins).HasMaxLength(200);

                entity.Property(e => e.InsArb)
                    .HasColumnName("Ins_Arb")
                    .HasMaxLength(200);

                entity.Property(e => e.LvlIndx).HasColumnName("Lvl_indx");

                entity.Property(e => e.NameArb)
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(20);

                entity.Property(e => e.NameEng)
                    .IsRequired()
                    .HasColumnName("Name_Eng")
                    .HasMaxLength(20);

                entity.Property(e => e.PreAlt).HasColumnName("Pre_Alt");

                entity.Property(e => e.PreCmpRqd).HasColumnName("Pre_Cmp_Rqd");

                entity.Property(e => e.PreStgId).HasColumnName("Pre_Stg_Id");

                entity.Property(e => e.PstCmpRqd).HasColumnName("Pst_Cmp_Rqd");

                entity.Property(e => e.PstStgId).HasColumnName("Pst_Stg_Id");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.SpclAlt).HasColumnName("Spcl_Alt");

                entity.Property(e => e.StgLvl).HasColumnName("Stg_Lvl");

                entity.Property(e => e.WfStgId).HasColumnName("WfStg_Id");

                entity.Property(e => e.WfStrcId).HasColumnName("WfStrc_Id");

                entity.HasOne(d => d.WfStg)
                    .WithMany(p => p.WfStrucStage)
                    .HasForeignKey(d => d.WfStgId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_WF_StrucStage_WF_Stage");

                entity.HasOne(d => d.WfStrc)
                    .WithMany(p => p.WfStrucStage)
                    .HasForeignKey(d => d.WfStrcId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_WF_StrucStage_WF_Structure");
            });

            modelBuilder.Entity<WfStrucStageAlert>(entity =>
            {
                entity.HasKey(e => e.StgAltId);

                entity.ToTable("WF_StrucStageAlert");

                entity.Property(e => e.StgAltId)
                    .HasColumnName("StgAlt_Id")
                    .ValueGeneratedNever();

                entity.Property(e => e.AltType).HasColumnName("Alt_Type");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Email).HasMaxLength(50);

                entity.Property(e => e.EmpId).HasColumnName("Emp_Id");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.StgId).HasColumnName("Stg_Id");

                entity.HasOne(d => d.Stg)
                    .WithMany(p => p.WfStrucStageAlert)
                    .HasForeignKey(d => d.StgId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_WF_StrucStageAlert_WF_StrucStage");
            });

            modelBuilder.Entity<WfStructure>(entity =>
            {
                entity.HasKey(e => e.WfStrcId);

                entity.ToTable("WF_Structure");

                entity.HasIndex(e => e.WfStrcpCode)
                    .IsUnique();

                entity.Property(e => e.WfStrcId).HasColumnName("WfStrc_Id");

                entity.Property(e => e.ActionAlt).HasColumnName("Action_Alt");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Desc).HasMaxLength(200);

                entity.Property(e => e.NameArb)
                    .HasColumnName("Name_Arb")
                    .HasMaxLength(20);

                entity.Property(e => e.NameEng)
                    .IsRequired()
                    .HasColumnName("Name_Eng")
                    .HasMaxLength(20);

                entity.Property(e => e.PreAlt).HasColumnName("Pre_Alt");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.SpclAlt).HasColumnName("Spcl_Alt");

                entity.Property(e => e.WfPlcTypId).HasColumnName("WfPlcTyp_Id");

                entity.Property(e => e.WfStrcpCode)
                    .IsRequired()
                    .HasColumnName("WfStrcp_Code")
                    .HasMaxLength(10);

                entity.HasOne(d => d.WfPlcTyp)
                    .WithMany(p => p.WfStructure)
                    .HasForeignKey(d => d.WfPlcTypId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_WF_Structure_WF_PolicyType");
            });

            OnModelCreatingPartial(modelBuilder);

            // ─────────────────────────────────────────────────────────────────
            // Leave Workflow Integration – new table entity configurations
            // ─────────────────────────────────────────────────────────────────

            modelBuilder.Entity<HrLvWfConfig>(entity =>
            {
                entity.HasKey(e => e.WfCfgId);
                entity.ToTable("HR_LvWfConfig");

                entity.Property(e => e.WfCfgId).HasColumnName("WfCfg_Id");
                entity.Property(e => e.LvTypId).HasColumnName("LvTyp_Id");

                entity.Property(e => e.WfCode)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.IsEnabled)
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.EffDate).HasColumnType("date");
                entity.Property(e => e.ExpDate).HasColumnType("date");

                entity.Property(e => e.ApproverConfig).HasColumnType("nvarchar(max)");
                entity.Property(e => e.PolicyRules).HasColumnType("nvarchar(max)");

                entity.Property(e => e.RecStat)
                    .HasColumnName("Rec_Stat")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.MdfId).HasColumnName("Mdf_Id");

                entity.Property(e => e.MdfDateTime)
                    .HasColumnName("Mdf_DateTime")
                    .HasColumnType("datetime");

                entity.HasOne(d => d.LvTyp)
                    .WithMany()
                    .HasForeignKey(d => d.LvTypId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HR_LvWfConfig_HR_LvType");
            });

            modelBuilder.Entity<HrLvWfConfig>(entity =>
            {
                entity.HasKey(e => e.WfCfgId);
                entity.ToTable("HrLvWfConfig");

                entity.Property(e => e.WfCfgId).HasColumnName("WfCfg_Id");
                entity.Property(e => e.LvTypId).HasColumnName("LvTyp_Id");
                entity.Property(e => e.WfCode).HasColumnName("WfCode");
                entity.Property(e => e.IsEnabled).HasColumnName("Is_Enabled");
                entity.Property(e => e.EffDate).HasColumnName("Eff_Date").HasColumnType("date");
                entity.Property(e => e.ExpDate).HasColumnName("Exp_Date").HasColumnType("date");
                entity.Property(e => e.ApproverConfig).HasColumnName("Approver_Config").HasColumnType("nvarchar(max)");
                entity.Property(e => e.PolicyRules).HasColumnName("Policy_Rules").HasColumnType("nvarchar(max)");
                entity.Property(e => e.RecStat).HasColumnName("Rec_Stat");
                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");
                entity.Property(e => e.CrtDateTime).HasColumnName("Crt_DateTime").HasColumnType("datetime");
                entity.Property(e => e.MdfId).HasColumnName("Mdf_Id");
                entity.Property(e => e.MdfDateTime).HasColumnName("Mdf_DateTime").HasColumnType("datetime");
            });

            modelBuilder.Entity<HrLvWfInstance>(entity =>
            {
                entity.HasKey(e => e.WfInstId);
                entity.ToTable("HrLvWfInstance");

                entity.Property(e => e.WfInstId).HasColumnName("WfInst_Id");
                entity.Property(e => e.LvId).HasColumnName("Lv_Id");
                entity.Property(e => e.WfInstanceId).HasColumnName("WfInstance_Id");
                entity.Property(e => e.WfInstanceNo).HasColumnName("WfInstance_No");
                entity.Property(e => e.WfStatus).HasColumnName("Wf_Status");
                entity.Property(e => e.CrtDateTime).HasColumnName("Crt_DateTime").HasColumnType("datetime");
                entity.Property(e => e.MdfDateTime).HasColumnName("Mdf_DateTime").HasColumnType("datetime");
            });

            modelBuilder.Entity<HrLvWfDelegation>(entity =>
            {
                entity.HasKey(e => e.DelegId);
                entity.ToTable("HrLvWfDelegation");

                entity.Property(e => e.DelegId).HasColumnName("Deleg_Id");
                entity.Property(e => e.DlgtrEmpId).HasColumnName("Dlgtr_Emp_Id");
                entity.Property(e => e.DlgteEmpId).HasColumnName("Dlgte_Emp_Id");
                entity.Property(e => e.LvTypId).HasColumnName("LvTyp_Id");
                entity.Property(e => e.FromDate).HasColumnType("date");
                entity.Property(e => e.ToDate).HasColumnType("date");
                entity.Property(e => e.Reason).HasMaxLength(500);
                entity.Property(e => e.WfDelegationId).HasColumnName("WfDeleg_Id");
                entity.Property(e => e.RecStat).HasColumnName("Rec_Stat");
                entity.Property(e => e.CrtId).HasColumnName("Crt_Id");
                entity.Property(e => e.CrtDateTime).HasColumnName("Crt_DateTime").HasColumnType("datetime");
                entity.Property(e => e.MdfId).HasColumnName("Mdf_Id");
                entity.Property(e => e.MdfDateTime).HasColumnName("Mdf_DateTime").HasColumnType("datetime");
            });

            modelBuilder.Entity<HrWfUserMapping>(entity =>
            {
                entity.HasKey(e => e.EmpSysId);
                entity.ToTable("HrWfUserMapping");

                entity.Property(e => e.EmpSysId).HasColumnName("Emp_Sys_Id");
                entity.Property(e => e.WfUserId).HasColumnName("WfUser_Id");
                entity.Property(e => e.WfUsername).HasColumnName("WfUsername").HasMaxLength(100);
                entity.Property(e => e.CreatedDate).HasColumnName("Created_Date").HasColumnType("datetime");
                entity.Property(e => e.ModifiedDate).HasColumnName("Modified_Date").HasColumnType("datetime");
            });

            modelBuilder.Entity<HrLvWfNotifyLog>(entity =>
            {
                entity.HasKey(e => e.LogId);
                entity.ToTable("HrLvWfNotifyLog");

                entity.Property(e => e.LogId).HasColumnName("LogId");
                entity.Property(e => e.LvId).HasColumnName("Lv_Id");
                entity.Property(e => e.WfInstId).HasColumnName("WfInst_Id");
                entity.Property(e => e.EventType).HasColumnName("EventType").HasMaxLength(50);
                entity.Property(e => e.ToEmpSysId).HasColumnName("To_Emp_Sys_Id");
                entity.Property(e => e.ToEmail).HasColumnName("ToEmail").HasMaxLength(100);
                entity.Property(e => e.Subject).HasColumnName("Subject").HasMaxLength(200);
                entity.Property(e => e.Body).HasColumnName("Body").HasColumnType("nvarchar(max)");
                entity.Property(e => e.IsSent).HasColumnName("IsSent");
                entity.Property(e => e.SentDateTime).HasColumnName("Sent_DateTime").HasColumnType("datetime");
                entity.Property(e => e.ErrorMsg).HasColumnName("ErrorMsg").HasMaxLength(500);
                entity.Property(e => e.RetryCount).HasColumnName("RetryCount");
                entity.Property(e => e.CrtDateTime).HasColumnName("Crt_DateTime").HasColumnType("datetime");
            });
        }


        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

        public DbSet<qHRM.Areas.Employee.Models.EmpSrtOrdVw> EmpSrtOdrVw { get; set; }
    }
}