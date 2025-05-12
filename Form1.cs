using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;    // 使用 IO 函式庫

namespace NotePad
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // 加入以下三行
            InitializeFontComboBox();
            InitializeFontSizeComboBox();
            InitializeFontStyleComboBox();
        }

        // 全域變數
        private Stack<string> undoStack = new Stack<string>(); //回復（undo）堆疊
        private Stack<string> redoStack = new Stack<string>(); //重作（redo）堆疊
        private const int MaxHistoryCount = 10; // 最多紀錄10個紀錄
        private bool isUndoRedo = false; // 是否為撤銷操作
        private int selectionStart = 0;                            // 記錄文字反白的起點
        private int selectionLength = 0;                           // 記錄文字反白的長度
        /*
          第二種方法使用:List<string>
        private List<string> textHistory = new List<string>();
        private const int MaxHistoryCount = 10;
        private bool isUndo = false;
        */

        private void btnOpen_Click(object sender, EventArgs e)
        {
            // 設置對話方塊標題
            openFileDialog1.Title = "選擇檔案";
            // 設置對話方塊篩選器，限制使用者只能選擇特定類型的檔案
            openFileDialog1.Filter = "Word檔案 (*.docx)|*.docx|文字檔案 (*.txt)|*.txt|所有檔案 (*.*)|*.*";
            // 如果希望預設開啟的檔案類型是文字檔案，可以這樣設置
            openFileDialog1.FilterIndex = 1;
            // 如果希望對話方塊在開啟時顯示的初始目錄，可以設置 InitialDirectory
            openFileDialog1.InitialDirectory = "C:\\";
            // 允許使用者選擇多個檔案
            openFileDialog1.Multiselect = true;

            // 顯示對話方塊，並等待使用者選擇檔案
            DialogResult result = openFileDialog1.ShowDialog();

            // 檢查使用者是否選擇了檔案
            if (result == DialogResult.OK)
            {
                // try-catch語法:當程式碼執行到try區塊時，若發生錯誤狀況，則會跳到catch區塊執行
                try
                {
                    // 使用者在OpenFileDialog選擇的檔案
                    string selectedFileName = openFileDialog1.FileName;  // 取得選擇的檔案位置

                    /*
                      // 第一種作法:使用 FileStream 打開檔案 (手動開關資源，基本概念)
                      // 建立一個檔案資料流，並且設定檔案名稱與檔案開啟模式為「開啟檔案」
                      FileStream fileStream = new FileStream(selectedFileName, FileMode.Open, FileAccess.Read);
                     // 讀取資料流
                      StreamReader streamReader = new StreamReader(fileStream);
                     // 將檔案內容顯示到 RichTextBox 中
                      rtbText.Text = streamReader.ReadToEnd();
                     // 關閉資料流與讀取資料流
                      fileStream.Close();
                      streamReader.Close();
                    */

                    // 第二種作法:使用 using 與 FileStream 打開檔案 使用 (using 語法可以自動管理資源，較通用)，using語法會自動關閉資源 = Close
                    using (FileStream fileStream = new FileStream(selectedFileName, FileMode.Open, FileAccess.Read))
                    {
                        // 使用 StreamReader 讀取檔案內容
                        using (StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8))
                        {
                            // 將檔案內容顯示到 RichTextBox 中
                            rtbText.Text = streamReader.ReadToEnd();
                        }
                    }

                    /* 
                      // 第三種作法:更為簡單的做法，將檔案內容顯示到 RichTextBox 中 (最簡潔的方式)
                      string fileContent = File.ReadAllText(selectedFileName);
                      rtbText.Text = fileContent;
                    */
                }
                catch (Exception ex)
                {   //MessageBox的語法：MessageBox.Show(要顯示的訊息, 視窗標題, 按鍵組合, 小圖示, 預設按鍵);

                    // 如果發生錯誤，用MessageBox顯示錯誤訊息
                    MessageBox.Show("讀取檔案時發生錯誤: " + ex.Message, "錯誤訊息", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // MessageBox顯示使用者關閉後的物件方塊，方塊介面可做更改
                // MessageBox.Show(要顯示的訊息, 視窗標題, 按鍵組合, 小圖示, 預設按鍵);
                MessageBox.Show("使用者取消了選擇檔案操作。", "訊息", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
            }
        }

        // 儲存檔案的程式碼
        private void btnSave_Click(object sender, EventArgs e)
        {
            // 設置對話方塊標題
            saveFileDialog1.Title = "儲存檔案";
            // 設置對話方塊篩選器，限制使用者只能選擇特定類型的檔案去做儲存
            saveFileDialog1.Filter = "Word檔案 (*.docx)|*.docx|文字檔案 (*.txt)|*.txt|所有檔案 (*.*)|*.*";
            // 如果希望預設儲存的檔案類型是文字檔案，可以這樣設置
            saveFileDialog1.FilterIndex = 1;
            // 如果希望對話方塊在開啟時顯示的初始目錄，可以設置 InitialDirectory
            saveFileDialog1.InitialDirectory = "C:\\";

            // 顯示對話方塊，並等待使用者指定儲存的檔案
            DialogResult result = saveFileDialog1.ShowDialog();

            //建立 FileStream 物件
            FileStream fileStream = null;  // null:在記憶體裡面先建立一個fileStream變數空間

            // 檢查使用者是否選擇了檔案
            if (result == DialogResult.OK)
            {
                try
                {
                    // 使用者指定的儲存檔案的路徑
                    string saveFileName = saveFileDialog1.FileName;

                    // 使用 FileStream 建立檔案，如果檔案已存在則覆寫
                    fileStream = new FileStream(saveFileName, FileMode.Create, FileAccess.Write);
                    // 將 RichTextBox 中的文字寫入檔案中
                    byte[] data = Encoding.UTF8.GetBytes(rtbText.Text);  // rtbText轉換為UTF8，接著在寫入data
                    fileStream.Write(data, 0, data.Length);

                    //// 使用 using 與 FileStream 建立檔案，如果檔案已存在則覆寫
                    //using (fileStream = new FileStream(saveFileName, FileMode.Create, FileAccess.Write))
                    //{
                    //    // 將 RichTextBox 中的文字寫入檔案中
                    //    byte[] data = Encoding.UTF8.GetBytes(rtbText.Text);
                    //    fileStream.Write(data, 0, data.Length);
                    //}

                    //// 將 RichTextBox 中的文字儲存到檔案中
                    //File.WriteAllText(saveFileName, rtbText.Text);

                    MessageBox.Show("檔案儲存成功。", "訊息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    // 如果發生錯誤，用 MessageBox 顯示錯誤訊息
                    MessageBox.Show("儲存檔案時發生錯誤: " + ex.Message, "錯誤訊息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                finally // 定義:finally區塊無論try區塊是否發生錯誤都會執行程式碼
                {
                    // 關閉資源，如果使用 using 或者直接以 File.WriteAllText 儲存文字檔，可以不需要。
                    fileStream.Close();
                }
            }
            else
            {
                MessageBox.Show("使用者取消了儲存檔案操作。", "訊息", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            }
        }

        private void rtbText_TextChanged(object sender, EventArgs e)
        {
            // 只有當isUndo這個變數是false的時候，才能堆疊文字編輯紀錄
            if (isUndoRedo == false)
            {
                undoStack.Push(rtbText.Text); // 將當前的文本內容加入堆疊
                redoStack.Clear();            // 清空重作堆疊

                // 確保堆疊中只保留最多10個紀錄
                if (undoStack.Count > MaxHistoryCount)
                {
                    // 用一個臨時堆疊，將除了最下面一筆的文字記錄之外，將文字紀錄堆疊由上而下，逐一移除再堆疊到臨時堆疊之中
                    Stack<string> tempStack = new Stack<string>();
                    for (int i = 0; i < MaxHistoryCount; i++)
                    {
                        tempStack.Push(undoStack.Pop());
                    }
                    undoStack.Clear(); // 清空堆疊
                                       // 文字編輯堆疊紀錄清空之後，再將暫存堆疊（tempStack）中的資料，逐一放回到文字編輯堆疊紀錄
                    foreach (string item in tempStack)
                    {
                        undoStack.Push(item);
                    }
                }
                UpdateListBox(); // 更新 ListBox
            }

            /*使用 List<string>
            if (!isUndo)
            {
                textHistory.Add(rtbText.Text);

                // 超過最大筆數時，移除最舊的一筆（第一筆）
                if (textHistory.Count > MaxHistoryCount)
                {
                    textHistory.RemoveAt(0);
                }

                UpdateListBox();
           */
        }

        // 更新 ListBox(函式)
        void UpdateListBox()
        {
            listUndo.Items.Clear(); // 清空 ListBox 中的元素

            // 將堆疊中的內容逐一添加到 ListBox 中
            foreach (string item in undoStack)
            {
                listUndo.Items.Add(item);
            }
        }

        // 初始化字體下拉選單
        private void InitializeFontComboBox()
        {
            // 將所有系統字體名稱添加到字體選擇框中
            foreach (FontFamily font in FontFamily.Families)
            {
                comboBoxFont.Items.Add(font.Name); //Items是(集合)
            }

            /*
            // 設置預設選中的項目為第一個字體(原本方法)
            comboBoxFont.SelectedIndex = 0;
            */
            //也可以這樣玩(額外方法)
            comboBoxFont.SelectedIndex = comboBoxFont.Items.Count -1;
        }

        // 初始化字體大小下拉選單
        private void InitializeFontSizeComboBox()
        {
            // 從8開始，每次增加2，直到72，將這些數值添加到字體大小選擇框中(迴圈做法)
            for (int i = 8; i <= 72; i += 2)
            {
                comboBoxSize.Items.Add(i); //Items是(集合)
            }
            /*(手動加入作法)
              comboBoxSize.Items.Add(8);
              comboBoxSize.Items.Add(10);
              comboBoxSize.Items.Add(12);
              '''                          //以此類推，在寫出32個
              comboBoxSize.Items.Add(72);
            */

            // 設置預設選中的項目為第三個大小，即12字體大小
            comboBoxSize.SelectedIndex = 2;
        }

        // 初始化字體樣式下拉選單(手動加入，不可用迴圈做)
        private void InitializeFontStyleComboBox()
        {
            // 將不同的字體樣式添加到字體樣式選擇框中(手動加入做法)
            comboBoxStyle.Items.Add(FontStyle.Regular.ToString());   // 正常(標準字體)
            comboBoxStyle.Items.Add(FontStyle.Bold.ToString());      // 粗體
            comboBoxStyle.Items.Add(FontStyle.Italic.ToString());    // 斜體
            comboBoxStyle.Items.Add(FontStyle.Underline.ToString()); // 底線
            comboBoxStyle.Items.Add(FontStyle.Strikeout.ToString()); // 刪除線
        /*(迴圈做法)                          
            foreach (FontStyle style in Enum.GetValues(typeof(FontStyle)))
            {
                comboBoxStyle.Items.Add(style.ToString());
            }
        */

            // 設置預設選中的項目為第一個樣式，即正常字體
            comboBoxStyle.SelectedIndex = 0;
        }
        // 這個方法在 comboBox 的選項變更時觸發
        private void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 保存當前選擇的文字起始位置和長度
            selectionStart = rtbText.SelectionStart;
            selectionLength = rtbText.SelectionLength;

            // 確保當前選擇的文字具有字型
            if (rtbText.SelectionFont != null)
            {
                // 從下拉選單中獲取選擇的字型、大小和樣式
                string selectedFont = comboBoxFont.SelectedItem?.ToString();
                string selectedSizeStr = comboBoxSize.SelectedItem?.ToString();
                string selectedStyleStr = comboBoxStyle.SelectedItem?.ToString();

                // 確保字型、大小和樣式都已選擇
                if (selectedFont != null && selectedSizeStr != null && selectedStyleStr != null)
                {
                    // 將選擇的大小字串轉換為浮點數
                    float selectedSize = float.Parse(selectedSizeStr);
                    // 將選擇的樣式字串轉換為 FontStyle 枚舉值
                    FontStyle selectedStyle = (FontStyle)Enum.Parse(typeof(FontStyle), selectedStyleStr);

                    // 獲取當前選擇的文字的字型
                    Font currentFont = rtbText.SelectionFont;
                    FontStyle newStyle = currentFont.Style;

                    // 檢查是否需要應用新的樣式，並更新樣式
                    if (comboBoxStyle.SelectedItem.ToString() == FontStyle.Bold.ToString())
                        newStyle = FontStyle.Bold;
                    else if (comboBoxStyle.SelectedItem.ToString() == FontStyle.Italic.ToString())
                        newStyle = FontStyle.Italic;
                    else if (comboBoxStyle.SelectedItem.ToString() == FontStyle.Underline.ToString())
                        newStyle = FontStyle.Underline;
                    else if (comboBoxStyle.SelectedItem.ToString() == FontStyle.Strikeout.ToString())
                        newStyle = FontStyle.Strikeout;
                    else
                        newStyle = FontStyle.Regular;

                    // 創建新的字型並應用到選擇的文字
                    Font newFont = new Font(selectedFont, selectedSize, newStyle);
                    rtbText.SelectionFont = newFont;
                }
            }

            // 恢復選擇狀態
            rtbText.Focus();
            rtbText.Select(selectionStart, selectionLength);
        }

        private void btnUndo_Click(object sender, EventArgs e)
        {
            if (undoStack.Count > 1)
            {
                isUndoRedo = true;
                redoStack.Push(undoStack.Pop()); // 將回復堆疊最上面的紀錄移出，再堆到重作堆疊
                rtbText.Text = undoStack.Peek(); // 將回復堆疊最上面一筆紀錄顯示
                UpdateListBox();
                isUndoRedo = false;
            }

                /*使用 List<string>
                isUndo = true;

                if (textHistory.Count > 1)
                {
                    // 移除目前的內容（最後一筆）
                    textHistory.RemoveAt(textHistory.Count - 1);

                    // 還原為上一筆內容
                    rtbText.Text = textHistory[^1]; // C# 8.0+ 語法，相當於 textHistory[textHistory.Count - 1]
                }

                UpdateListBox();
                isUndo = false;
                */
        }

        private void btnRedo_Click(object sender, EventArgs e)
        {
            if (redoStack.Count > 0)
            {
                isUndoRedo = true;
                undoStack.Push(redoStack.Pop()); // 將重作堆疊最上面的紀錄移出，再堆到回復堆疊
                rtbText.Text = undoStack.Peek(); // 將回復堆疊最上面一筆紀錄顯示
                UpdateListBox();
                isUndoRedo = false;
            }
        }
    } 
}

