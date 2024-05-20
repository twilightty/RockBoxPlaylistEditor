using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RockBoxPlaylistEditor
{

    public partial class Form1 : Form
    {

        string path;
        string path2;

        public Form1()
        {
            InitializeComponent();
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(
 string lpFileName,
 uint dwDesiredAccess,
 uint dwShareMode,
 IntPtr SecurityAttributes,
 uint dwCreationDisposition,
 uint dwFlagsAndAttributes,
 IntPtr hTemplateFile
);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped
        );

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            byte[] lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private IntPtr handle = IntPtr.Zero;

        const uint GENERIC_READ = 0x80000000;
        const uint GENERIC_WRITE = 0x40000000;
        const int FILE_SHARE_READ = 0x1;
        const int FILE_SHARE_WRITE = 0x2;
        const int FSCTL_LOCK_VOLUME = 0x00090018;
        const int FSCTL_DISMOUNT_VOLUME = 0x00090020;
        const int IOCTL_STORAGE_EJECT_MEDIA = 0x2D4808;
        const int IOCTL_STORAGE_MEDIA_REMOVAL = 0x002D4804;

        /// <summary>
        /// Constructor for the USBEject class
        /// </summary>
        /// <param name="driveLetter">This should be the drive letter. Format: F:/, C:/..</param>

        public IntPtr USBEject(string driveLetter)
        {
            string filename = @"\\.\" + driveLetter[0] + ":";
            return CreateFile(filename, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, 0x3, 0, IntPtr.Zero);
        }

        public bool Eject(IntPtr handle)
        {
            bool result = false;

            if (LockVolume(handle) && DismountVolume(handle))
            {
                PreventRemovalOfVolume(handle, false);
                result = AutoEjectVolume(handle);
            }
            CloseHandle(handle);
            return result;
        }

        private bool LockVolume(IntPtr handle)
        {
            uint byteReturned;

            for (int i = 0; i < 10; i++)
            {
                if (DeviceIoControl(handle, FSCTL_LOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out byteReturned, IntPtr.Zero))
                {
                    System.Windows.Forms.MessageBox.Show("Eject completed!");
                    return true;
                }
                Thread.Sleep(500);
            }
            return false;
        }

        private bool PreventRemovalOfVolume(IntPtr handle, bool prevent)
        {
            byte[] buf = new byte[1];
            uint retVal;

            buf[0] = (prevent) ? (byte)1 : (byte)0;
            return DeviceIoControl(handle, IOCTL_STORAGE_MEDIA_REMOVAL, buf, 1, IntPtr.Zero, 0, out retVal, IntPtr.Zero);
        }

        private bool DismountVolume(IntPtr handle)
        {
            uint byteReturned;
            return DeviceIoControl(handle, FSCTL_DISMOUNT_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out byteReturned, IntPtr.Zero);
        }

        private bool AutoEjectVolume(IntPtr handle)
        {
            uint byteReturned;
            return DeviceIoControl(handle, IOCTL_STORAGE_EJECT_MEDIA, IntPtr.Zero, 0, IntPtr.Zero, 0, out byteReturned, IntPtr.Zero);
        }

        private bool CloseVolume(IntPtr handle)
        {
            return CloseHandle(handle);
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("Are you sure to exit?", "Confirmation", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                //Environment.Exit(0);
                e.Cancel = false;
            }
            else
            {
                // your Code for Changes or anything you want to allow user changes etc.
                e.Cancel = true;
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                string meow = string.Join(string.Empty, file.Skip(2)).Replace("\\", "/");
                this.dataGridView1.Rows.Add(meow);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "playlist files (*.m3u8)|*.m3u8";
            dialog.Title = "Load m3u8 playlist";
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                this.dataGridView1.Rows.Clear();


                textBox1.Text = dialog.FileName;

                path2 = dialog.FileName;

                var fileStream = dialog.OpenFile();



                using (StreamReader reader = new StreamReader(fileStream))
                {
                    path = reader.ReadToEnd();
                    Console.WriteLine(path);

                    using (StringReader reader2 = new StringReader(path))
                    {
                        string line;
                        while ((line = reader2.ReadLine()) != null)
                        {
                            this.dataGridView1.Rows.Add(line);
                        }
                    }
                }
            }
        }



        private void button2_Click(object sender, EventArgs e)
        {
            Eject(USBEject(textBox1.Text.Substring(0, 2)));
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog2 = new OpenFileDialog();
            dialog2.Filter = "All files (*.*)|*.*";
            dialog2.Title = "Load songs for playlist";
            dialog2.Multiselect = true;

            if (dialog2.ShowDialog() == DialogResult.OK)
            {
                var fileStream = dialog2.OpenFile();

                string[] allFiles = dialog2.FileNames;

                foreach (string file in allFiles)
                {
                    string meow = string.Join(string.Empty, file.Skip(2)).Replace("\\", "/");
                    this.dataGridView1.Rows.Add(meow);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

            Console.WriteLine(this.dataGridView1.Rows.Count);

            try
            {
                using (TextWriter tw = new StreamWriter(path2))
                {
                    for (int i = 0; i < this.dataGridView1.Rows.Count; i++)
                    {
                        tw.WriteLine($"{dataGridView1.Rows[i].Cells[0].Value.ToString()}");
                    }
                }

                MessageBox.Show("Done!", "Data written successfully", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
            } catch (Exception asdf) {
                MessageBox.Show(asdf.Message, "Something went wrong", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            DeleteSelectedRows();
        }

        private void DeleteSelectedRows()
        {
            HashSet<int> rowsToDelete = new HashSet<int>();

            // Collect all row indices that contain selected cells
            foreach (DataGridViewCell selectedCell in dataGridView1.SelectedCells)
            {
                if (selectedCell.Selected)
                {
                    rowsToDelete.Add(selectedCell.RowIndex);
                }
            }

            // Remove rows starting from the highest index to avoid index shifting issues
            List<int> sortedRows = new List<int>(rowsToDelete);
            sortedRows.Sort((a, b) => b.CompareTo(a)); // Sort in descending order

            foreach (int rowIndex in sortedRows)
            {
                dataGridView1.Rows.RemoveAt(rowIndex);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {
            MoveSelectedRow(-1); //Moveup
        }

        private void MoveSelectedRow(int direction)
        {
            // Exit if no cell is selected or multiple cells are selected
            if (dataGridView1.SelectedCells.Count != 1)
            {
                MessageBox.Show("Please select a single cell to move its row.");
                return;
            }

            // Get the selected cell
            DataGridViewCell selectedCell = dataGridView1.SelectedCells[0];
            int rowIndex = selectedCell.RowIndex;
            int newRowIndex = rowIndex + direction;

            // Exit if the new row index is out of bounds
            if (newRowIndex < 0 || newRowIndex >= dataGridView1.Rows.Count)
            {
                return;
            }

            // Swap the rows
            DataGridViewRow selectedRow = dataGridView1.Rows[rowIndex];
            dataGridView1.Rows.Remove(selectedRow);
            dataGridView1.Rows.Insert(newRowIndex, selectedRow);

            // Reselect the moved cell
            dataGridView1.ClearSelection();
            dataGridView1.Rows[newRowIndex].Cells[selectedCell.ColumnIndex].Selected = true;
        }
        private void button8_Click(object sender, EventArgs e)
        {
            MoveSelectedRow(1); //Move down
        }
    }
}
