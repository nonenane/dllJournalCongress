using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace dllJournalCongress
{
    public partial class frmMain : Form
    {
        private DataTable dtData;
        private bool isChangeValue = false;
        private DateTime _dateStart, _dateEnd;
        public frmMain()
        {
            InitializeComponent();
            dgvData.AutoGenerateColumns = false;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            dtpStart.Value = DateTime.Now.AddMonths(-1);
            dtpEnd.Value = DateTime.Now.AddMonths(2);


            Task<DataTable> task = Config.hCntMain.getObjectLease(true);
            task.Wait();
            DataTable dtObjectLease = task.Result;

            cmbObject.DisplayMember = "cName";
            cmbObject.ValueMember = "id";
            cmbObject.DataSource = dtObjectLease;

            _dateStart = dtpStart.Value.Date;
            _dateEnd = dtpEnd.Value.Date;

            isChangeValue = false;
            getData();
        }

        private void btUpdate_Click(object sender, EventArgs e)
        {
            getData();
        }

        private void getData()
        {
            Task<DataTable> task = Config.hCntMain.getJournalCongress(dtpStart.Value.Date, dtpEnd.Value.Date);
            task.Wait();
            dtData = task.Result.Copy();
            task = null;

            setFilter();
            dgvData.DataSource = dtData;
            isChangeValue = false;
        }

        private void setFilter()
        {
            if (dtData == null || dtData.Rows.Count == 0)
            {
                //btEdit.Enabled = btDelete.Enabled = false;                
                return;
            }

            try
            {
                string filter = "";

                if (tbLandLord.Text.Trim().Length != 0)
                    filter += (filter.Length == 0 ? "" : " and ") + $"nameLandLord like '%{tbLandLord.Text.Trim()}%'";

                if (tbTenant.Text.Trim().Length != 0)
                    filter += (filter.Length == 0 ? "" : " and ") + $"nameTenant like '%{tbTenant.Text.Trim()}%'";

                if (tbAgreement.Text.Trim().Length != 0)
                    filter += (filter.Length == 0 ? "" : " and ") + $"Agreement like '%{tbAgreement.Text.Trim()}%'";

                if (tbNamePlace.Text.Trim().Length != 0)
                    filter += (filter.Length == 0 ? "" : " and ") + $"namePlace like '%{tbNamePlace.Text.Trim()}%'";

                if ((int)cmbObject.SelectedValue != 0)
                    filter += (filter.Length == 0 ? "" : " and ") + $"id_ObjectLease  = {cmbObject.SelectedValue}";


                string strFilter = "("
                    + "((isLinkPetitionLeave = 0 AND isConfirmed = 0) " +
                    "OR (isLinkPetitionLeave = 1 AND isConfirmed_LinkPetitionLeave = 0) " +
                    "OR (isCancelAgreements is null AND isConfirmed = 1 AND isConfirmed_LinkPetitionLeave = 0))";

                if (chbCongressAccept.Checked) {
                    strFilter += $" OR (isLinkPetitionLeave = 0 AND isConfirmed = 1)";
                }
                if (chbDropAgreements.Checked) {
                    strFilter += $" OR ((isLinkPetitionLeave = 1 AND isConfirmed_LinkPetitionLeave = 1) OR (isCancelAgreements is not null AND isConfirmed = 1 ))";
                }
                strFilter += ")";

                filter += (filter.Length == 0 ? "" : " and ") + strFilter;

                dtData.DefaultView.RowFilter = filter;
                dtData.DefaultView.Sort = "nameLandLord asc, nameTenant asc, nameObject asc";
            }
            catch
            {
                dtData.DefaultView.RowFilter = "id = -1";
            }
            finally
            {
                //btEdit.Enabled = btDelete.Enabled =
                //dtData.DefaultView.Count != 0;
                dgvData_SelectionChanged(null, null);
            }
        }

        private void dgvData_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;
            //Рисуем рамку для выделеной строки
            if (dgv.Rows[e.RowIndex].Selected)
            {
                int width = dgv.Width;
                Rectangle r = dgv.GetRowDisplayRectangle(e.RowIndex, false);
                Rectangle rect = new Rectangle(r.X, r.Y, width - 1, r.Height - 1);

                ControlPaint.DrawBorder(e.Graphics, rect,
                    SystemColors.Highlight, 2, ButtonBorderStyle.Solid,
                    SystemColors.Highlight, 2, ButtonBorderStyle.Solid,
                    SystemColors.Highlight, 2, ButtonBorderStyle.Solid,
                    SystemColors.Highlight, 2, ButtonBorderStyle.Solid);
            }
        }

        private void dgvData_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex != -1 && dtData != null && dtData.DefaultView.Count != 0)
            {

                Color rColor = Color.White;
                if (!(bool)dtData.DefaultView[e.RowIndex]["isLinkPetitionLeave"] && (bool)dtData.DefaultView[e.RowIndex]["isConfirmed"])
                    rColor = panel2.BackColor;

                if ((bool)dtData.DefaultView[e.RowIndex]["isLinkPetitionLeave"] && (bool)dtData.DefaultView[e.RowIndex]["isConfirmed_LinkPetitionLeave"])
                    rColor = panel3.BackColor;
                else if (dtData.DefaultView[e.RowIndex]["isCancelAgreements"]!=DBNull.Value && (bool)dtData.DefaultView[e.RowIndex]["isConfirmed"])
                    rColor = panel3.BackColor;

                dgvData.Rows[e.RowIndex].DefaultCellStyle.BackColor = rColor;
                dgvData.Rows[e.RowIndex].DefaultCellStyle.SelectionBackColor = rColor;

                dgvData.Rows[e.RowIndex].DefaultCellStyle.SelectionForeColor = Color.Black;

                if ((bool)dtData.DefaultView[e.RowIndex]["isLinkPetitionLeave"])
                    dgvData.Rows[e.RowIndex].Cells[Date_of_Departure.Index].Style.BackColor =
                         dgvData.Rows[e.RowIndex].Cells[Date_of_Departure.Index].Style.SelectionBackColor = panel1.BackColor;
            }
        }

        private void dtpStart_ValueChanged(object sender, EventArgs e)
        {
            if (dtpStart.Value.Date > dtpEnd.Value.Date)
                dtpEnd.Value = dtpStart.Value.Date;

            isChangeValue = _dateStart.Date!=dtpStart.Value.Date;
        }

        private void dtpEnd_ValueChanged(object sender, EventArgs e)
        {
            if (dtpStart.Value.Date > dtpEnd.Value.Date)
                dtpStart.Value = dtpEnd.Value.Date;

            isChangeValue = _dateEnd.Date != dtpEnd.Value.Date;
        }

        private void dtpStart_CloseUp(object sender, EventArgs e)
        {
            if (isChangeValue)
                getData();
        }

        private void cmbObject_SelectionChangeCommitted(object sender, EventArgs e)
        {
            setFilter();
        }

        private void dgvData_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            tbLandLord.Location = new Point(dgvData.Location.X, tbLandLord.Location.Y);
            tbLandLord.Size = new Size(nameLandLord.Width, tbLandLord.Height);
            
            tbTenant.Location = new Point(dgvData.Location.X+ nameLandLord.Width+1, tbLandLord.Location.Y);
            tbTenant.Size = new Size(nameTenant.Width, tbLandLord.Height);

            tbAgreement.Location = new Point(dgvData.Location.X + nameLandLord.Width + nameTenant.Width + nameObject.Width + 1, tbLandLord.Location.Y);
            tbAgreement.Size = new Size(Agreement.Width, tbLandLord.Height);

            tbNamePlace.Location = new Point(dgvData.Location.X + nameLandLord.Width + nameTenant.Width + nameObject.Width + Agreement.Width + 1, tbLandLord.Location.Y);
            tbNamePlace.Size = new Size(namePlace.Width, tbLandLord.Height);
        }

        private void chbDropAgreements_CheckedChanged(object sender, EventArgs e)
        {
            setFilter();
        }

        private void tbLandLord_TextChanged(object sender, EventArgs e)
        {
            setFilter();
        }

        private void dgvData_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvData.CurrentRow == null || dgvData.CurrentRow.Index == -1 || dtData == null || dtData.DefaultView.Count == 0 || dgvData.CurrentRow.Index >= dtData.DefaultView.Count)
            {
                btPrint.Enabled = false;
                btAcceptD.Enabled = false;
                return;
            }

            btPrint.Enabled = true;

            btAcceptD.Enabled = !(bool)dtData.DefaultView[dgvData.CurrentRow.Index]["isConfirmed"] 
                || (!(bool)dtData.DefaultView[dgvData.CurrentRow.Index]["isConfirmed_LinkPetitionLeave"] && (bool)dtData.DefaultView[dgvData.CurrentRow.Index]["isLinkPetitionLeave"]);
        }

        private void btAcceptD_Click(object sender, EventArgs e)
        {
            if (dgvData.CurrentRow != null && dgvData.CurrentRow.Index != -1 && dtData != null && dtData.DefaultView.Count != 0)
            {
                int id = (int)dtData.DefaultView[dgvData.CurrentRow.Index]["id"];
                bool isLinkPetitionLeave = (bool)dtData.DefaultView[dgvData.CurrentRow.Index]["isLinkPetitionLeave"];
                if(isLinkPetitionLeave) id = (int)dtData.DefaultView[dgvData.CurrentRow.Index]["id_LinkPetitionLeave"];
            }
        }

        private void dtpStart_Leave(object sender, EventArgs e)
        {
            if (isChangeValue)
                getData();
        }
    }
}
