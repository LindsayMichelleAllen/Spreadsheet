using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using CptS321.Spreadsheet.SpreadsheetEngine;
using CptS321;



namespace Spreadsheet
{
    public partial class Form1 : Form
    {
        private Cell clickedCell;
        private Spreadsheet sheet;
        public Form1()
        {
            InitializeComponent();
            // Add rows & columns
            for (int index = 0; index < 26; index++)
            {
                char letter = Convert.ToChar('A' + index);  //converts acii value
                string header = letter.ToString();
                dataGridView1.Columns.Add(header, header);
            }
            for (int index = 0; index < 50; index++)
            {
                DataGridViewRow row = new DataGridViewRow();
                row.HeaderCell.Value = (index + 1).ToString();
                dataGridView1.Rows.Add(row);
            }
            dataGridView1.RowHeadersWidth = 21; //fits 2 integers
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.sheet = new Spreadsheet(50, 26);
            sheet.CellPropertyChanged += Spreadsheet_CellPropertyChanged; //subscribe to event
            Controls.Add(textBox1);
        }
        private void Spreadsheet_CellPropertyChanged(object sender, EventArgs e)
        {
            var currCell = (Cell)sender;    //cast sender
            //assign inner spreadsheet value to GUI value
            dataGridView1.Rows[currCell.RowIndex].Cells[currCell.ColumnIndex].Value = currCell.Value;
        }
        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var currCell = this.sheet.GetCell(e.RowIndex, e.ColumnIndex);   //inner spreadsheet
            var dataCell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex]; //GUI spreadsheet
            if(dataCell.Value != null)
            {
                currCell.Text = dataCell.Value.ToString();  //save change to inner spreadsheet
            }
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            textBox1.KeyDown += new KeyEventHandler(OnKeyDownHandler);
        }
        public void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            var updatedText = (TextBox)sender;
            if (e.KeyCode == Keys.Enter)
            {
                clickedCell.Text = updatedText.Text;
            }
        }
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            clickedCell = sheet.GetCell(e.RowIndex, e.ColumnIndex);
            if (textBox1 != null)
            {
                textBox1.Text = clickedCell.Text;
            }
        }
        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog1 = new OpenFileDialog())
            {
                DialogResult response = openFileDialog1.ShowDialog();   // code reference: https://www.dotnetperls.com/openfiledialog
                
                if (response == DialogResult.OK)
                {
                    string fileName = openFileDialog1.FileName;
                    using (StreamReader fileInput = new StreamReader(fileName))
                    {
                        var convert = new XMLHandler();
                        convert.LoadCells(fileInput);   //loads cellDict
                        //sheet.ClearSpreadsheet();       //clear spreadsheet before loading cells
                        var cellDict = convert.cellDict;
                                //cellDict = { {"cellName" :[__text__, __value__]} , 
                                //             {"cellName" :[__text__, __value__]} }
                        foreach(var cell in cellDict)
                        {
                            //cell = {"cellName" : [__text__, __value__]}
                            string cellName = cell.Key;
                            var textValList = cell.Value;
                            var backend = sheet.GetCell(cellName);
                            backend.Text = textValList[0];
                            backend.Value = textValList[1]; //takes care of evaluation until dependancy is changed
                        }
                    }
                }
                else if (response == DialogResult.Cancel)
                {
                    return;
                }
                else
                {
                    MessageBox.Show("Unable to open file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: create record of cells that do not contain null values
            IDictionary<string, List<string>> saveCellDict = new Dictionary<string, List<string>>();
            for (int row = 0; row < sheet.RowCount; row++)
            {
                for (int col = 0; col < sheet.ColumnCount; col++)
                {
                    if (sheet.GetCell(row, col).Text != null)
                    {
                        List<string> saveList = new List<string>();
                        var cell = sheet.GetCell(row, col);
                        saveList.Add(cell.Text);
                        saveList.Add(cell.Value);
                        var name = Char.ToUpper((char)(cell.ColumnIndex + 65)).ToString();
                        name = name + (row + 1).ToString();
                        saveCellDict.Add(name, saveList);                        
                    }
                }
            }

            using (SaveFileDialog saveFileDialog1 = new SaveFileDialog())
            {
                saveFileDialog1.Filter = "Text File|*.txt"; //only allow text files
                var response = saveFileDialog1.ShowDialog();
                if (saveFileDialog1.FileName != "")
                {
                    using (StreamWriter saveInput = new StreamWriter(saveFileDialog1.OpenFile()))
                    {
                        var convert = new XMLHandler();
                        convert.SaveCells(saveInput, saveCellDict);
                    }
                }
                else if (response == DialogResult.Cancel)
                {
                    return;
                }
                else
                {
                    MessageBox.Show("Unable to save file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void startDemoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.sheet.StartDemo();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About form2 = new About();
            form2.Text = "About";
            form2.ShowDialog();
        }
    }
    public class Spreadsheet
    {
        private int _RowCount = 0;
        public int RowCount
        {
            get { return _RowCount; }
            set { _RowCount = value; }
        }
        private int _ColumnCount = 0;
        public int ColumnCount
        {
            get { return _ColumnCount; }
            set { _ColumnCount = value; }
        }
        private newCell[,] sheet;
        public event PropertyChangedEventHandler CellPropertyChanged;
        private class newCell : Cell
        {
            ExpTree tree;            
            public newCell(int row, int column) : base(row, column)
            {
            }
            public void SetText(string newText)
            {
                this.Text = newText;
            }
            public void SetValue(string newVal)
            {
                this.Value = newVal;
            }
            public double evalTree()
            {
                return tree.Eval();
            }
            public ExpTree getTree()
            {
                return this.tree;
            }
            public void setTree(ExpTree newTree)
            {
                this.tree = newTree;
            }
            public void unsubscribe(newCell reference)
            {
                reference.PropertyChanged -= ReEval;
            }
            public void subscribe(newCell reference)
            {
                reference.PropertyChanged += ReEval;
            }
            public void ReEval(object sender, PropertyChangedEventArgs e) //DEPENDENT CELL (holding formula)
            {
                if (e.PropertyName == "Value")
                {
                    var cell = sender as newCell;
                    var row = cell.RowIndex;
                    //get col letter
                    var variable = Char.ToUpper((char)(cell.ColumnIndex + 65)).ToString();      
                    //concat with row#
                    variable = variable + (row + 1).ToString();      
                    //change the var value in tree dict
                    getTree().SetVar(variable, Convert.ToDouble(cell.Value));
                    //reEval with new dict value
                    this.Value = getTree().Eval().ToString();
                }
            }
        }
        public Spreadsheet(int rows, int columns)
        {
            RowCount = rows;
            ColumnCount = columns;
            sheet = new newCell[rows, columns]; //create inner spreadsheet
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    var cell = new newCell(i, j);   //create new cell
                    sheet[i, j] = cell;             //assign to array position for inner spreadsheet
                    sheet[i, j].PropertyChanged += Spreadsheet_PropertyChanged; //subscribe to event
                }
            }
        }
        private void Spreadsheet_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var currCell = (newCell)sender;
            currCell.SetValue(CalculateValue(currCell));

            if (CellPropertyChanged != null)
            {
                CellPropertyChanged(sender, e); //trigger event
            }
        }
        private string CalculateValue(newCell cell)
        {
            bool invalidRef = false;
            if (cell.Text[0] != '=')    //check for function
            {
                cell.SetValue(cell.Text);
                return cell.Value;
            }
            if (cell.getTree() != null) // remove old dependencies when new tree is created
            {
                foreach (var variable in cell.getTree().getVarList())
                {
                    if (variable.Length >= 2 && char.IsDigit(variable[1]))
                    {
                        cell.unsubscribe(GetCell(variable) as newCell);
                    }
                }
            }
            //remove "="
            var expression = cell.Text.Substring(1);
            cell.setTree(new ExpTree(expression));
            foreach (var variable in cell.getTree().getVarList())
            {
                // check for valid column entry
                if (variable.Length >= 2 && char.IsDigit(variable[1]))
                {
                    cell.subscribe(GetCell(variable) as newCell);
                    cell.getTree().SetVar(variable, Convert.ToDouble(GetCell(variable).Value));
                }
                else   //bad cell reference
                {
                    invalidRef = true;
                }
            }
            if (invalidRef)
            {
                cell.SetValue("#REF");
            }
            else
            {
                cell.SetValue(cell.getTree().Eval().ToString());
            }
            return cell.Value;             
        }
        public Cell GetCell(int rowIndex, int columnIndex)
        {
            if (sheet[rowIndex, columnIndex] != null)
            {
                return sheet[rowIndex, columnIndex];
            }
            else
            {
                return null;
            }
        }
        public Cell GetCell(string contents) 
        {
            //extract row and column
            int column = (int)contents[0] % 32; //code reference: https://stackoverflow.com/a/20045091
            int row = Convert.ToInt32(contents.Substring(1));
            return GetCell((row-1), (column-1)); 
        }
        public void ClearSpreadsheet()
        {
            for (int i = 0; i < this.RowCount; i++)
            {
                for (int x = 0; x < this.ColumnCount; x++)
                {
                    if(this.GetCell(i, x).Text != null)
                    {
                        //flag to unsubscribe before changing the values
                        this.GetCell(i, x).Text = null;
                        this.GetCell(i, x).Value = null;
                    }
                }

            }
        }
        public void StartDemo()
        {
            // 50 random 'Hello World!'s
            Random rand = new Random();
            for (int i = 0; i < 50; i++)
            {
                int row = rand.Next(50); //generate number 0-49 for row
                int column = rand.Next(2, 26);  //exclude first two columns (used in test 2 & 3)
                this.GetCell(row, column).Text = "Hello World!";
            }
            // column B
            for (int i = 0; i < this.RowCount; i++)
            {
                this.GetCell(i, 1).Text = "This is Cell B" + (i + 1).ToString();
            }
            // column A = column B
            for (int i = 0; i < this.RowCount; i++)
            {
                this.GetCell(i, 0).Text = GetCell(i, 1).Value;
            }
        }
    }    
}


