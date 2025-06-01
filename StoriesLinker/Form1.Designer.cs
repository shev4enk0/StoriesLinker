namespace StoriesLinker
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.proj_path_label = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.proj_name_label = new System.Windows.Forms.Label();
            this.proj_name_value = new System.Windows.Forms.Label();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.chapters_count_label = new System.Windows.Forms.Label();
            this.chapters_count_value = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.path_value = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImage = global::StoriesLinker.Properties.Resources._512x512;
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBox1.Location = new System.Drawing.Point(13, 13);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(100, 100);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(241, 19);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(268, 20);
            this.textBox1.TabIndex = 1;
            // 
            // proj_path_label
            // 
            this.proj_path_label.AutoSize = true;
            this.proj_path_label.Location = new System.Drawing.Point(131, 22);
            this.proj_path_label.Name = "proj_path_label";
            this.proj_path_label.Size = new System.Drawing.Size(86, 13);
            this.proj_path_label.TabIndex = 2;
            this.proj_path_label.Text = "Папка проекта:";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(515, 17);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(72, 22);
            this.button1.TabIndex = 3;
            this.button1.Text = "Обзор...";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // proj_name_label
            // 
            this.proj_name_label.AutoSize = true;
            this.proj_name_label.Location = new System.Drawing.Point(131, 69);
            this.proj_name_label.Name = "proj_name_label";
            this.proj_name_label.Size = new System.Drawing.Size(76, 13);
            this.proj_name_label.TabIndex = 4;
            this.proj_name_label.Text = "Имя проекта:";
            // 
            // proj_name_value
            // 
            this.proj_name_value.AutoSize = true;
            this.proj_name_value.Location = new System.Drawing.Point(241, 69);
            this.proj_name_value.Name = "proj_name_value";
            this.proj_name_value.Size = new System.Drawing.Size(10, 13);
            this.proj_name_value.TabIndex = 7;
            this.proj_name_value.Text = "-";
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(21, 163);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(271, 33);
            this.button3.TabIndex = 11;
            this.button3.Text = "Сформировать таблицы локализациии";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.GenerateLocalizTables);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(21, 202);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(271, 33);
            this.button4.TabIndex = 12;
            this.button4.Text = "Сформировать папки для упаковки бандлов";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.GenerateOutputFolderForBundles);
            // 
            // textBox2
            // 
            this.textBox2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.textBox2.Enabled = false;
            this.textBox2.Location = new System.Drawing.Point(321, 163);
            this.textBox2.Multiline = true;
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(255, 81);
            this.textBox2.TabIndex = 13;
            this.textBox2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // chapters_count_label
            // 
            this.chapters_count_label.AutoSize = true;
            this.chapters_count_label.Location = new System.Drawing.Point(131, 92);
            this.chapters_count_label.Name = "chapters_count_label";
            this.chapters_count_label.Size = new System.Drawing.Size(70, 13);
            this.chapters_count_label.TabIndex = 14;
            this.chapters_count_label.Text = "Кол-во глав:";
            // 
            // chapters_count_value
            // 
            this.chapters_count_value.Location = new System.Drawing.Point(241, 92);
            this.chapters_count_value.MaxLength = 2;
            this.chapters_count_value.Name = "chapters_count_value";
            this.chapters_count_value.Size = new System.Drawing.Size(79, 20);
            this.chapters_count_value.TabIndex = 15;
            this.chapters_count_value.Text = "1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(131, 47);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 13);
            this.label1.TabIndex = 16;
            this.label1.Text = "Текущий путь:";
            // 
            // path_value
            // 
            this.path_value.AutoSize = true;
            this.path_value.Location = new System.Drawing.Point(241, 47);
            this.path_value.Name = "path_value";
            this.path_value.Size = new System.Drawing.Size(10, 13);
            this.path_value.TabIndex = 17;
            this.path_value.Text = "-";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(599, 317);
            this.Controls.Add(this.path_value);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.chapters_count_value);
            this.Controls.Add(this.chapters_count_label);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.proj_name_value);
            this.Controls.Add(this.proj_name_label);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.proj_path_label);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.pictureBox1);
            this.Name = "Form1";
            this.Text = "Компоновщик Stories: Your Choice";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label proj_path_label;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label proj_name_label;
        private System.Windows.Forms.Label proj_name_value;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label chapters_count_label;
        private System.Windows.Forms.TextBox chapters_count_value;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label path_value;
    }
}