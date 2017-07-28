using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace primeNumberLite
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //フォームが読み込まれた処理
        private void Form1_Load(object sender, EventArgs e)
        {
            //スパコン向けに一応...ね?(エラーで動かないけどねｗ)
            int core = Environment.ProcessorCount;
            if (core < 16384)//816384=2^23 (*1024するとint超える)
            {
                numericUpDown2.Maximum = core * 1024;
            }
            else
            {
                numericUpDown2.Maximum = 2147483647;//int数の最大
            }
        }

        //テキストボックスの中身をコピー
        private void button2_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBox1.Text);
        }

        //リスト化モードになったとき
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            checkBox1.Enabled = true;
            if (checkBox1.Checked)
            {
                numericUpDown2.Enabled = true;
            }
        }

        //判別モードになったとき
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            checkBox1.Enabled = false;
            numericUpDown2.Enabled = false;
        }

        //マルチタスク化ボタンのチェックが変わったとき
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown2.Enabled = !numericUpDown2.Enabled;
        }

        //計算処理(メインプロセス)
        public void button1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            textBox1.Text = null;
            progressBar1.Value = 0;
            try
            {
                sw.Start();
                ulong n = (ulong)numericUpDown1.Value;

                //素数判別モード
                if (radioButton1.Checked)
                {
                    if (n == 2)
                    {
                        sw.Stop();
                        label9.Text = "素数です。(" + (int)sw.Elapsed.TotalMilliseconds + "ms)";
                        progressBar1.Value = 100;
                    }
                    else if (n % 2 == 0)
                    {
                        sw.Stop();
                        label9.Text = "素数ではありません。(" + (int)sw.Elapsed.TotalMilliseconds + "ms)";
                        textBox1.Text = "2";
                        progressBar1.Value = 100;
                    }
                    else
                    {
                        double sqrt = Math.Sqrt(n);
                        long result = 0;
                        int forProgress = (int)(sqrt - 3) / 100;
                        for (long i = 3; i <= sqrt; i += 2)
                        {
                            if (n % (ulong)i == 0)
                            {
                                result = i;
                                break;
                            }

                            if (i > forProgress * (progressBar1.Value + 1) && progressBar1.Value < 100)
                            {
                                progressBar1.Value += 1;
                            }
                        }

                        if (result == 0)
                        {
                            sw.Stop();
                            label9.Text = "素数です。(" + (int)sw.Elapsed.TotalMilliseconds + "ms)";
                            progressBar1.Value = 100;
                        }
                        else
                        {
                            sw.Stop();
                            label9.Text = "素数ではありません。(" + (int)sw.Elapsed.TotalMilliseconds + "ms)";
                            textBox1.Text = $"{result}";
                            progressBar1.Value = 100;
                        }
                    }

                }//リストモード
                else
                {

                    //マルチスレッドを選択してないとき(旧プログラム)
                    if (!checkBox1.Checked)
                    {
                        textBox1.Text = "2\r\n";
                        int forProgress = (int)(n - 3) / 100;
                        int[] primeLiteList = new int[21];
                        for (ulong k = 3; k <= n; k += 2)
                        {

                            double sqrt = Math.Sqrt(k);
                            long result = 0;
                            for (long i = 3; i <= sqrt; i += 2)
                            {
                                if (k % (ulong)i == 0)
                                {
                                    result = i;
                                    break;
                                }
                            }

                            if (result == 0)
                            {
                                textBox1.Text = textBox1.Text + k + "\r\n";
                            }


                            if (k > (ulong)(forProgress * (progressBar1.Value + 1)) && progressBar1.Value < 100)
                            {
                                progressBar1.Value += 1;
                            }
                        }
                        sw.Stop();
                        int time = (int)sw.Elapsed.TotalMilliseconds;
                        if (sw.Elapsed.TotalHours > 1)
                        {
                            label9.Text = "リスト化が終了しました。(" + sw.Elapsed.Hours + "h"
                                + sw.Elapsed.Minutes + "m"
                                + sw.Elapsed.Seconds + "s)";
                        }
                        else if (time > 60000)
                        {
                            label9.Text = "リスト化が終了しました。(" + sw.Elapsed.Minutes + "m" +
                                sw.Elapsed.Seconds + "s)";
                        }
                        else if (time > 10000)
                        {
                            label9.Text = "リスト化が終了しました。(" + (int)sw.Elapsed.TotalSeconds + "s)";
                        }
                        else
                        {
                            label9.Text = "リスト化が終了しました。(" + (int)sw.Elapsed.TotalMilliseconds + "ms)";
                        }
                        progressBar1.Value = 100;


                    }
                    else
                    {
                        //マルチスレッド有効化時
                        int Threads = (int)numericUpDown2.Value;
                        textBox1.Text = "2\r\n";
                        int forProgress = (int)(n - 3) / 100;

                        if (n > 2)
                        {
                            int boost = Threads / 1024;
                            ulong p = 3;
                            bool end = false;//一時停止ボタン用?
                            while (!end)
                            {
                                var tr = ParentTask(p, Threads, n);
                                tr.Start();
                                textBox1.Text = textBox1.Text + tr.Result;

                                //p += (ulong)(Threads*boost*2);
                                p += (ulong)Threads * 2;

                                if (p > (ulong)(forProgress * (progressBar1.Value + 1)) && progressBar1.Value < 100)
                                {
                                    progressBar1.Value += 1;
                                }

                                if (p > n)
                                {
                                    end = true;
                                    break;
                                }
                            }
                        }

                        sw.Stop();
                        int time = (int)sw.Elapsed.TotalMilliseconds;
                        if (sw.Elapsed.TotalHours > 1)
                        {
                            label9.Text = "リスト化が終了しました。(" + sw.Elapsed.Hours + "h"
                                + sw.Elapsed.Minutes + "m"
                                + sw.Elapsed.Seconds + "s)";
                        }
                        else if (time > 60000)
                        {
                            label9.Text = "リスト化が終了しました。(" + sw.Elapsed.Minutes + "m" +
                                sw.Elapsed.Seconds + "s)";
                        }
                        else if (time > 10000)
                        {
                            label9.Text = "リスト化が終了しました。(" + (int)sw.Elapsed.TotalSeconds + "s)";
                        }
                        else
                        {
                            label9.Text = "リスト化が終了しました。(" + (int)sw.Elapsed.TotalMilliseconds + "ms)";
                        }
                        progressBar1.Value = 100;


                    }

                }

            }
            catch(Exception ex)
            {
                label9.Text = "エラー(ErrorMessageは右)";
                textBox1.Text = ex.Message;
            }
        }

        //さらなる親タスク(実験段階につき未実装)
        public String GrandParentTask(ulong p , int Threads,int boost, ulong n)
        {
            String result = "";
            Task[] tasks = new Task[boost];
            for (int i=0; i<boost; i++)
            {
                var task = ParentTask(p, Threads, n);
                task.Start();
                tasks[i] = task;
                result = result + task.Result;
                p += (ulong)Threads * 2;
            }
            Task.WaitAll(tasks);
            return result;
        }

        //loop
        public Task<String> ParentTask(ulong p, int Threads, ulong n)
        {
            String result = "";
            Task[] tasks = new Task[Threads];//8byte * Threads 1Gとすると限界は64threadが限界?(ではないみたい)
            List<ulong> results = new List<ulong>();

            for (int t = 0; t < Threads; t++)
            {
                //tasks[t] = Task<ulong>.Factory.StartNew(() => PrimeTask(p));
                var task = new Task<ulong>(() => PrimeTask(p));
                task.Start();
                tasks[t] = task;
                results.Add(task.Result);
                p += 2;
            }
            Task.WaitAll(tasks);

            results.Sort();
            foreach (ulong q in results)
            {
                if (q != 0)
                {
                    if (q > n)
                    {
                        break;
                    }
                    else
                    {
                        result = result + q + "\r\n";
                    }
                }
            }

            var LastResult = new Task<String>(() =>{ return result; });
            return LastResult;

        }

        //Task (素数じゃないなら0が返る)
        public ulong PrimeTask(ulong p)
        {
            ulong result = 0;
            if (WetherPrime(p))
            {
                result = p;
            }
            return result;
        } 

        //素数判別処理 (素数ならtrue)
        public bool WetherPrime(ulong p)
        {
            bool notPrime = false;

            if (p % 2 == 0)
            {
                if (p != 2)
                {
                    notPrime = true;
                }
            }
            else if(p != 3)
            {
                double sqrt = Math.Sqrt(p);
                for (long i = 3; i <= sqrt; i += 2)
                {
                    if (p % (ulong)i == 0)
                    {
                        notPrime = true;
                        break;
                    }
                }
            }

            return !notPrime;
        }

    }
}
