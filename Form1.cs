using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WordVideoGenerator.Models;
using WordVideoGenerator.Services;

namespace WordVideoGenerator
{
    public partial class Form1 : Form
    {
        private readonly ApiService _apiService;
        private readonly VideoService _videoService;
        private readonly string _defaultOutputFolder;
        private string _outputFolder;

        public Form1()
        {
            InitializeComponent();
            _apiService = new ApiService();
            _videoService = new VideoService();
            
            // 设置默认输出路径
            _defaultOutputFolder = Path.Combine(Application.StartupPath, "OutputVideos");
            _outputFolder = _defaultOutputFolder;
            txtOutputPath.Text = _outputFolder;
            
            // 确保默认输出目录存在
            Directory.CreateDirectory(_outputFolder);

            // 初始化日志控件
            LogService.Instance.SetLogTextBox(txtLog);
            LogService.Instance.Log("程序启动完成");
        }

        private void btnSelectPath_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "选择视频保存路径";
                dialog.UseDescriptionForTitle = true;
                dialog.SelectedPath = _outputFolder;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _outputFolder = dialog.SelectedPath;
                    txtOutputPath.Text = _outputFolder;
                    Directory.CreateDirectory(_outputFolder);
                    LogService.Instance.Log($"已更改输出目录: {_outputFolder}");
                }
            }
        }

        private async void btnGenerate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtInput.Text))
            {
                MessageBox.Show("请输入单词！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnGenerate.Enabled = false;
            btnSelectPath.Enabled = false;
            progressBar.Value = 0;
            progressBar.Maximum = 100;

            try
            {
                var words = ExtractEnglishWords(txtInput.Text);
                progressBar.Maximum = words.Count * 100;
                LogService.Instance.Log($"开始处理 {words.Count} 个单词");

                foreach (var word in words)
                {
                    var wordInfo = new WordInfo
                    {
                        Word = word,
                        OutputVideoPath = Path.Combine(_outputFolder, $"{word}.mp4")
                    };

                    // 检查文件是否已存在
                    if (File.Exists(wordInfo.OutputVideoPath))
                    {
                        var result = MessageBox.Show(
                            $"文件 {word}.mp4 已存在，是否覆盖？",
                            "文件已存在",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question
                        );

                        if (result == DialogResult.No)
                        {
                            LogService.Instance.Log($"跳过已存在的文件: {word}.mp4");
                            progressBar.Value += 100;
                            continue;
                        }
                        else
                        {
                            LogService.Instance.Log($"将覆盖已存在的文件: {word}.mp4");
                        }
                    }

                    // 1. 下载音频
                    UpdateStatus($"正在下载 {word} 的音频...");
                    wordInfo.AudioPath = await _videoService.DownloadAudioAsync(word);
                    progressBar.Value += 20;

                    // 2. 获取翻译
                    UpdateStatus($"正在获取 {word} 的翻译...");
                    wordInfo.Translations = await _apiService.GetWordTranslationsAsync(word);
                    progressBar.Value += 20;

                    // 3. 生成图片
                    UpdateStatus($"正在生成 {word} 的图片...");
                    wordInfo.ImagePath = await _apiService.GenerateImageAsync(word);
                    progressBar.Value += 20;

                    // 4. 生成视频
                    UpdateStatus($"正在生成 {word} 的视频...");
                    await _videoService.GenerateVideoAsync(wordInfo);
                    progressBar.Value += 40;

                    // 清理临时文件
                    if (File.Exists(wordInfo.AudioPath))
                        File.Delete(wordInfo.AudioPath);
                    if (File.Exists(wordInfo.ImagePath))
                        File.Delete(wordInfo.ImagePath);
                }

                LogService.Instance.Log("所有视频生成完成");
                MessageBox.Show($"视频生成完成！\n保存路径：{_outputFolder}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogService.Instance.LogError("生成视频时发生错误", ex);
                MessageBox.Show($"发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGenerate.Enabled = true;
                btnSelectPath.Enabled = true;
                UpdateStatus("就绪");
            }
        }

        private List<string> ExtractEnglishWords(string input)
        {
            var words = new List<string>();
            var regex = new Regex(@"\b[A-Za-z]+\b");
            var matches = regex.Matches(input);

            foreach (Match match in matches)
            {
                words.Add(match.Value.ToLower());
            }

            return words;
        }

        private void UpdateStatus(string status)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action(() => lblStatus.Text = status));
            }
            else
            {
                lblStatus.Text = status;
            }
        }
    }
}
