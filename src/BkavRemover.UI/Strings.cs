using System;
using System.Collections.Generic;

namespace BkavRemover.UI
{
    internal static class Strings
    {
        public enum Lang { En, Vi }

        private static Lang _current = Lang.En;
        public static Lang Current
        {
            get { return _current; }
            set { _current = value; EventHandler h = LanguageChanged; if (h != null) h(null, EventArgs.Empty); }
        }

        public static event EventHandler LanguageChanged;

        public static string Get(string key)
        {
            Dictionary<string, string> dict = _current == Lang.Vi ? Vi : En;
            string val;
            return dict.TryGetValue(key, out val) ? val : key;
        }

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            // Nav
            {"nav.scan", "Scan"},
            {"nav.report", "Report Issue"},
            {"nav.donate", "Donate"},
            {"nav.about", "About"},
            {"nav.lang", "VI"},

            // Dashboard
            {"dash.title", "Bkav Pro / BluPro Remover"},
            {"dash.subtitle", "Complete detection & removal toolkit"},
            {"dash.scanning", "  Scanning...  "},
            {"dash.clean", "  System Clean  "},
            {"dash.safemode", "  Safe Mode  "},
            {"dash.normalmode", "  Normal Mode  "},
            {"dash.normalmode.kernel", "  Normal Mode - Kernel driver  "},
            {"dash.card.services", "SERVICES / DRIVERS"},
            {"dash.card.processes", "PROCESSES"},
            {"dash.card.files", "FILES & FOLDERS"},
            {"dash.card.registry", "REGISTRY ENTRIES"},
            {"dash.detected", "Detected items"},
            {"dash.detected.n", "Detected items ({0})"},
            {"dash.filter.all", "All Types"},
            {"dash.filter.services", "Services"},
            {"dash.filter.processes", "Processes"},
            {"dash.filter.files", "Files"},
            {"dash.filter.registry", "Registry"},
            {"dash.filter.detail", "{0} ({1} of {2})"},
            {"dash.selectall", "Select All"},
            {"dash.deselectall", "Deselect All"},
            {"dash.export", "Export"},
            {"dash.scaninfo.scanning", "Scanning..."},
            {"dash.scaninfo.done", "Scanned in {0:F1}s"},
            {"dash.btn.scan", "Scan Again"},
            {"dash.btn.clean", "Clean Selected"},
            {"dash.btn.clean.n", "Clean Selected ({0})"},
            {"dash.autosafemode", "Reboot into Safe Mode automatically when needed"},
            {"dash.cleanstate.title", "System is Clean"},
            {"dash.cleanstate.sub", "No Bkav or BluPro components detected on this system."},

            // Cleaning
            {"clean.title", "Removing Bkav Pro / BluPro"},
            {"clean.btn.working", "Working..."},
            {"clean.btn.back", "Back to Dashboard"},
            {"clean.prepare", "Phase 1 of 2 - Disabling services & preparing Safe Mode reboot..."},
            {"clean.safe", "Phase 2 of 2 - Cleaning in Safe Mode..."},
            {"clean.normal", "Cleaning in Normal Mode (no kernel driver)..."},

            // Report Issue
            {"report.title", "Report an Issue"},
            {"report.subtitle", "Found a bug or something didn't get removed? Let us know."},
            {"report.sysinfo", "System Info (auto-included in report)"},
            {"report.subject", "Subject"},
            {"report.description", "Description"},
            {"report.attachlog", "Include the run log from this session"},
            {"report.btn.save", "Save Report"},
            {"report.btn.log", "Open Log Folder"},
            {"report.hint.nogit", "The GitHub issue tracker isn't published yet - this saves a report bundle to your Desktop."},
            {"report.hint.git", "This opens the GitHub issue tracker in your browser."},
            {"report.saved.title", "Report saved"},

            {"report.nolog", "No log directory found yet."},
            {"report.btn.reports", "Open Reports"},
            {"report.pending", "{0} pending report(s) saved in:\n{1}"},
            {"report.pending.none", "No pending reports."},
            {"report.noreports", "No reports saved yet."},
            {"report.saved.msg", "Report saved to:\n{0}\n\nWhen the GitHub repository is published, pending reports can be submitted as issues."},

            // Donate
            {"donate.icon", "DONATE"},
            {"donate.title", "Support this project"},
            {"donate.body", "This tool is free and open-source. If it saved you time\nor helped you remove Bkav, consider buying the\ndeveloper a coffee. Every bit helps!"},
            {"donate.btn", "Donate at nguyenthanhdev.com/donate"},
            {"donate.thanks", "Thank you for your support!"},

            // About
            {"about.title", "About & Help"},
            {"about.appname", "Bkav Pro / BluPro Remover"},
            {"about.sysinfo", "System Information"},
            {"about.howworks", "How it works"},
            {"about.howbody", "1.  Scan - Detects all Bkav/BluPro services, processes, files,\n" +
                              "     scheduled tasks and registry entries.\n" +
                              "2.  Prepare - Disables services, enables Safe Mode boot.\n" +
                              "3.  Clean (Safe Mode) - Kills processes, deletes files,\n" +
                              "     removes services and registry entries.\n" +
                              "4.  Verify - Rescans to confirm complete removal."},
            {"about.whysafe", "Why Safe Mode?"},
            {"about.whybody", "Bkav uses kernel-level self-protection (PPL) that prevents process " +
                              "termination and file deletion in Normal Mode. Safe Mode disables " +
                              "these drivers, allowing complete removal."},
            {"about.dev", "Developed by Nguyen Thanh  |  nguyenthanhdev.com"},

            // Dialogs
            {"dlg.error", "Error"},
            {"dlg.info", "Info"},
            {"dlg.scanfail", "Scan failed: {0}"},
            {"dlg.confirm.safemode.title", "Automatic Bkav Removal"},
            {"dlg.confirm.safemode", "Bkav kernel driver detected. The tool will:\n\n" +
                "1. Disable Bkav services & drivers\n" +
                "2. Reboot into Safe Mode (automatic)\n" +
                "3. Clean all Bkav files, registry & tasks\n" +
                "4. Reboot back to Normal Mode (automatic)\n" +
                "5. Verify removal\n\n" +
                "The entire process is automatic after you click Yes.\nContinue?"},
            {"dlg.confirm.clean.title", "Confirm removal"},
            {"dlg.confirm.clean", "No active kernel driver found. Clean can run directly.\n\n" +
                "This will permanently remove {0} selected item(s): services, " +
                "processes, files, scheduled tasks and registry entries.\n\nThis cannot be undone. Continue?"},
            {"dlg.confirm.manual", "This will permanently remove {0} selected item(s): services, " +
                "processes, files, scheduled tasks and registry entries. This cannot be undone.\n\nContinue?"},
            {"dlg.export.title", "Export Scan Results"},
            {"dlg.export.done", "Scan results exported to:\n{0}"},
            {"dlg.export.done.title", "Export Complete"},
            {"dlg.export.fail", "Failed to export: {0}"},
        };

        private static readonly Dictionary<string, string> Vi = new Dictionary<string, string>
        {
            // Nav
            {"nav.scan", "Quet"},
            {"nav.report", "Bao loi"},
            {"nav.donate", "Ung ho"},
            {"nav.about", "Gioi thieu"},
            {"nav.lang", "EN"},

            // Dashboard
            {"dash.title", "Bkav Pro / BluPro Remover"},
            {"dash.subtitle", "Cong cu phat hien & go bo hoan toan"},
            {"dash.scanning", "  Dang quet...  "},
            {"dash.clean", "  He thong sach  "},
            {"dash.safemode", "  Safe Mode  "},
            {"dash.normalmode", "  Binh thuong  "},
            {"dash.normalmode.kernel", "  Binh thuong - Co driver nhan  "},
            {"dash.card.services", "DICH VU / DRIVER"},
            {"dash.card.processes", "TIEN TRINH"},
            {"dash.card.files", "FILE & THU MUC"},
            {"dash.card.registry", "REGISTRY"},
            {"dash.detected", "Muc phat hien"},
            {"dash.detected.n", "Muc phat hien ({0})"},
            {"dash.filter.all", "Tat ca"},
            {"dash.filter.services", "Dich vu"},
            {"dash.filter.processes", "Tien trinh"},
            {"dash.filter.files", "File"},
            {"dash.filter.registry", "Registry"},
            {"dash.filter.detail", "{0} ({1} / {2})"},
            {"dash.selectall", "Chon tat ca"},
            {"dash.deselectall", "Bo chon tat ca"},
            {"dash.export", "Xuat"},
            {"dash.scaninfo.scanning", "Dang quet..."},
            {"dash.scaninfo.done", "Quet trong {0:F1}s"},
            {"dash.btn.scan", "Quet lai"},
            {"dash.btn.clean", "Don dep"},
            {"dash.btn.clean.n", "Don dep ({0})"},
            {"dash.autosafemode", "Tu dong khoi dong vao Safe Mode khi can"},
            {"dash.cleanstate.title", "He thong sach"},
            {"dash.cleanstate.sub", "Khong phat hien thanh phan Bkav hoac BluPro tren he thong."},

            // Cleaning
            {"clean.title", "Dang go bo Bkav Pro / BluPro"},
            {"clean.btn.working", "Dang xu ly..."},
            {"clean.btn.back", "Ve trang chinh"},
            {"clean.prepare", "Buoc 1/2 - Vo hieu hoa dich vu & chuan bi khoi dong Safe Mode..."},
            {"clean.safe", "Buoc 2/2 - Don dep trong Safe Mode..."},
            {"clean.normal", "Don dep o che do binh thuong (khong co driver nhan)..."},

            // Report Issue
            {"report.title", "Bao loi"},
            {"report.subtitle", "Phat hien loi hoac go bo chua sach? Hay cho chung toi biet."},
            {"report.sysinfo", "Thong tin he thong (tu dong dinh kem)"},
            {"report.subject", "Tieu de"},
            {"report.description", "Mo ta"},
            {"report.attachlog", "Dinh kem nhat ky phien lam viec"},
            {"report.btn.save", "Luu bao cao"},
            {"report.btn.log", "Mo thu muc log"},
            {"report.hint.nogit", "GitHub issue tracker chua duoc tao - bao cao se duoc luu vao Desktop."},
            {"report.hint.git", "Se mo GitHub issue tracker tren trinh duyet."},
            {"report.saved.title", "Da luu bao cao"},

            {"report.nolog", "Chua co thu muc log."},
            {"report.btn.reports", "Mo thu muc bao cao"},
            {"report.pending", "{0} bao cao dang cho tai:\n{1}"},
            {"report.pending.none", "Chua co bao cao nao."},
            {"report.noreports", "Chua co bao cao nao."},
            {"report.saved.msg", "Da luu bao cao tai:\n{0}\n\nKhi GitHub repository duoc tao, cac bao cao se duoc gui len issues."},

            // Donate
            {"donate.icon", "UNG HO"},
            {"donate.title", "Ho tro du an"},
            {"donate.body", "Cong cu nay mien phi va ma nguon mo. Neu no giup ban\ntiet kiem thoi gian hoac go bo Bkav thanh cong,\nhay mua cho tac gia mot ly ca phe!"},
            {"donate.btn", "Ung ho tai nguyenthanhdev.com/donate"},
            {"donate.thanks", "Cam on ban da ung ho!"},

            // About
            {"about.title", "Gioi thieu & Huong dan"},
            {"about.appname", "Bkav Pro / BluPro Remover"},
            {"about.sysinfo", "Thong tin he thong"},
            {"about.howworks", "Cach hoat dong"},
            {"about.howbody", "1.  Quet - Phat hien tat ca dich vu, tien trinh, file,\n" +
                              "     tac vu dinh ky va registry cua Bkav/BluPro.\n" +
                              "2.  Chuan bi - Vo hieu hoa dich vu, bat Safe Mode.\n" +
                              "3.  Don dep (Safe Mode) - Diet tien trinh, xoa file,\n" +
                              "     go dich vu va registry.\n" +
                              "4.  Kiem tra - Quet lai de xac nhan da go hoan toan."},
            {"about.whysafe", "Tai sao can Safe Mode?"},
            {"about.whybody", "Bkav su dung bao ve cap nhan (PPL) ngan chan viec diet tien trinh " +
                              "va xoa file o che do binh thuong. Safe Mode vo hieu hoa " +
                              "cac driver nay, cho phep go bo hoan toan."},
            {"about.dev", "Phat trien boi Nguyen Thanh  |  nguyenthanhdev.com"},

            // Dialogs
            {"dlg.error", "Loi"},
            {"dlg.info", "Thong tin"},
            {"dlg.scanfail", "Quet that bai: {0}"},
            {"dlg.confirm.safemode.title", "Tu dong go bo Bkav"},
            {"dlg.confirm.safemode", "Phat hien driver nhan Bkav. Cong cu se:\n\n" +
                "1. Vo hieu hoa dich vu & driver Bkav\n" +
                "2. Khoi dong vao Safe Mode (tu dong)\n" +
                "3. Xoa tat ca file, registry & tac vu Bkav\n" +
                "4. Khoi dong lai binh thuong (tu dong)\n" +
                "5. Kiem tra ket qua\n\n" +
                "Toan bo qua trinh tu dong sau khi ban nhan Yes.\nTiep tuc?"},
            {"dlg.confirm.clean.title", "Xac nhan go bo"},
            {"dlg.confirm.clean", "Khong phat hien driver nhan. Co the don dep truc tiep.\n\n" +
                "Se go bo vinh vien {0} muc da chon: dich vu, " +
                "tien trinh, file, tac vu dinh ky va registry.\n\nKhong the hoan tac. Tiep tuc?"},
            {"dlg.confirm.manual", "Se go bo vinh vien {0} muc da chon: dich vu, " +
                "tien trinh, file, tac vu dinh ky va registry. Khong the hoan tac.\n\nTiep tuc?"},
            {"dlg.export.title", "Xuat ket qua quet"},
            {"dlg.export.done", "Da xuat ket qua quet tai:\n{0}"},
            {"dlg.export.done.title", "Xuat thanh cong"},
            {"dlg.export.fail", "Xuat that bai: {0}"},
        };
    }
}
