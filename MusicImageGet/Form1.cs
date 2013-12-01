                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    namespace MusicImageGet
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Windows.Forms;
    using System.Xml;

    public partial class  Form1 : Form
    {
        private Mutex getTask = new Mutex();
        private int nThread = 2;
        private Queue<Task> queTask = new Queue<Task>();
        private Thread[] threads;

        public static Form1 WinForm = null;

        public Form1()
        {
            WinForm = this;
            this.InitializeComponent();
        }

        private void BeginWork()
        {
            int num;
            if (this.threads != null)
            {
                for (num = 0; num < this.nThread; num++)
                {
                    if (this.threads[num].ThreadState == ThreadState.Running)
                    {
                        MessageBox.Show("必须等待全部线程终止..");
                        return;
                    }
                }
            }

            for (int i = 0; i < this.listView1.Items.Count; i++)
            {
                if (listView1.Items[i].SubItems[1].ToString() != "处理成功")
                {
                    Task item = new Task
                    {
                        filepath = this.listView1.Items[i].Text,
                        index = i
                    };
                    this.FListUpdateFunction("等待处理...", i);
                    this.queTask.Enqueue(item);
                }
            }

            this.threads = new Thread[this.nThread];
            for (num = 0; num < this.nThread; num++)
            {
                this.threads[num] = new Thread(new ThreadStart(this.WorkThread));
                this.threads[num].Start();
            }
        }

        public void FListUpdateFunction(string str, int n)
        {
            if (this.listView1.InvokeRequired)
            {
                FlushClient_FList method = new FlushClient_FList(this.FListUpdateFunction);
                base.Invoke(method, new object[] { str, n });
            }
            else
            {
                listView1.Items[n].SubItems[1].Text = str; 
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }


        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                string[] data = (string[]) e.Data.GetData(DataFormats.FileDrop);
                foreach (string str in data)
                {
                    ListViewItem item = new ListViewItem {
                        Text = str
                    };
                    item.SubItems.Add("");
                    this.listView1.Items.Add(item);
                }
            }
        }

        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move | DragDropEffects.Copy | DragDropEffects.Scroll;
        }

        private void listView1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move | DragDropEffects.Copy | DragDropEffects.Scroll;
        }

        private void WorkThread()
        {
            while (true)
            {
                this.getTask.WaitOne();
                if (this.queTask.Count == 0)
                {
                    this.getTask.ReleaseMutex();
                    return;
                }
                Task task = this.queTask.Dequeue();
                this.getTask.ReleaseMutex();
                try
                {
                    this.FListUpdateFunction("开始处理", task.index);
                    new MusicFactory(task.filepath).UpdateMusicCovr(task.index);
                    this.FListUpdateFunction("处理成功", task.index);
                }
                catch (Exception exception)
                {
                    this.FListUpdateFunction(exception.Message, task.index);
                }
            }
        }

        private void 开始ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.BeginWork();
        }

        public delegate void FlushClient_FList(string str, int n);

        private class Task
        {
            public string filepath;
            public int index;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(threads != null)
            {
                for (int num = 0; num < this.nThread; num++)
                {
                    if (this.threads[num].ThreadState == ThreadState.Running)
                        this.threads[num].Abort();
                }
            }
            Application.Exit();
        }

        private void 支持m4ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}

                                                                                                                                                                                                                                                                                                                                                        