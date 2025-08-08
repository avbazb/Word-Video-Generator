namespace WordVideoGenerator;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        txtInput = new TextBox();
        btnGenerate = new Button();
        progressBar = new ProgressBar();
        lblStatus = new Label();
        label1 = new Label();
        txtOutputPath = new TextBox();
        btnSelectPath = new Button();
        label2 = new Label();
        txtLog = new TextBox();
        label3 = new Label();
        SuspendLayout();
        // 
        // txtInput
        // 
        txtInput.Location = new Point(12, 32);
        txtInput.Multiline = true;
        txtInput.Name = "txtInput";
        txtInput.Size = new Size(460, 110);
        txtInput.TabIndex = 0;
        // 
        // btnGenerate
        // 
        btnGenerate.Location = new Point(12, 188);
        btnGenerate.Name = "btnGenerate";
        btnGenerate.Size = new Size(460, 35);
        btnGenerate.TabIndex = 1;
        btnGenerate.Text = "生成视频";
        btnGenerate.UseVisualStyleBackColor = true;
        btnGenerate.Click += btnGenerate_Click;
        // 
        // progressBar
        // 
        progressBar.Location = new Point(12, 229);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(460, 23);
        progressBar.TabIndex = 2;
        // 
        // lblStatus
        // 
        lblStatus.AutoSize = true;
        lblStatus.Location = new Point(12, 255);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(37, 17);
        lblStatus.TabIndex = 3;
        lblStatus.Text = "就绪";
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(12, 12);
        label1.Name = "label1";
        label1.Size = new Size(225, 17);
        label1.TabIndex = 4;
        label1.Text = "请输入英文单词（每行一个或空格分隔）：";
        // 
        // txtOutputPath
        // 
        txtOutputPath.Location = new Point(12, 160);
        txtOutputPath.Name = "txtOutputPath";
        txtOutputPath.ReadOnly = true;
        txtOutputPath.Size = new Size(379, 23);
        txtOutputPath.TabIndex = 5;
        // 
        // btnSelectPath
        // 
        btnSelectPath.Location = new Point(397, 159);
        btnSelectPath.Name = "btnSelectPath";
        btnSelectPath.Size = new Size(75, 25);
        btnSelectPath.TabIndex = 6;
        btnSelectPath.Text = "选择...";
        btnSelectPath.UseVisualStyleBackColor = true;
        btnSelectPath.Click += btnSelectPath_Click;
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(12, 142);
        label2.Name = "label2";
        label2.Size = new Size(92, 17);
        label2.TabIndex = 7;
        label2.Text = "视频保存路径：";
        // 
        // txtLog
        // 
        txtLog.Location = new Point(12, 297);
        txtLog.Multiline = true;
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.Size = new Size(460, 150);
        txtLog.TabIndex = 8;
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new Point(12, 277);
        label3.Name = "label3";
        label3.Size = new Size(68, 17);
        label3.TabIndex = 9;
        label3.Text = "运行日志：";
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(484, 461);
        Controls.Add(label3);
        Controls.Add(txtLog);
        Controls.Add(label2);
        Controls.Add(btnSelectPath);
        Controls.Add(txtOutputPath);
        Controls.Add(label1);
        Controls.Add(lblStatus);
        Controls.Add(progressBar);
        Controls.Add(btnGenerate);
        Controls.Add(txtInput);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "单词视频生成器";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TextBox txtInput;
    private Button btnGenerate;
    private ProgressBar progressBar;
    private Label lblStatus;
    private Label label1;
    private TextBox txtOutputPath;
    private Button btnSelectPath;
    private Label label2;
    private TextBox txtLog;
    private Label label3;
}
