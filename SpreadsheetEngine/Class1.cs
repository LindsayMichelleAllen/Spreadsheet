using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.ComponentModel;


namespace CptS321
{
    public class XMLHandler
    {
        public IDictionary<string, List<string>> cellDict = new Dictionary<string, List<string>>();
        public XMLHandler()
        {
            
        }
        public void LoadCells(StreamReader loadInput)
        {
            //move this code to a spreadsheet helper function
            XmlDocument document = new XmlDocument();
            document.Load(loadInput);
            XmlNode root = document.FirstChild;
            if (root.HasChildNodes)
            {
                for (int i = 0; i < root.ChildNodes.Count; i++)
                {
                    List<string> tempList = new List<string>(); //reusing list caused invalid values to be saved in dictionary
                    var data = root.ChildNodes[i].InnerXml;
                    string cellName = root.ChildNodes[i].SelectSingleNode("name").InnerXml;
                    string cellText = root.ChildNodes[i].SelectSingleNode("text").InnerXml;
                    tempList.Add(cellText);
                    string cellValue = root.ChildNodes[i].SelectSingleNode("value").InnerXml;
                    tempList.Add(cellValue);
                    this.cellDict.Add(cellName, tempList);                    
                }
            }
        }
        public void SaveCells(StreamWriter saveInput, IDictionary<string, List<string>> saveCellDict)
        {
            XmlDocument document = new XmlDocument();
            XmlNode cells = document.CreateElement("cells");
            document.AppendChild(cells);
            foreach (var _cell in saveCellDict)
            {               
                XmlNode cell = document.CreateElement("cell");
                document.AppendChild(cells).AppendChild(cell);
                XmlNode name = document.CreateElement("name");
                name.InnerText = _cell.Key; //get title of cell being saved
                document.AppendChild(cells).AppendChild(cell).AppendChild(name);
                XmlNode text = document.CreateElement("text");
                text.InnerText = _cell.Value[0]; //get text contents of cell being saved
                document.AppendChild(cells).AppendChild(cell).AppendChild(text);
                XmlNode value = document.CreateElement("value");
                value.InnerText = _cell.Value[1]; //get value contents of cell being saved
                document.AppendChild(cells).AppendChild(cell).AppendChild(value);
            }
            document.Save(saveInput); //should be saving finished XML here
        }
    }
    public class ExpTree
    {
        List<string> varList = new List<string>();
        Dictionary<string, double> varDict = new Dictionary<string, double>();
        Node root;
        List<string> operators = new List<string>(new string[] { "+", "-", "/", "*" });
        public abstract class Node
        {
            public string val = "";
            public abstract double Eval(Dictionary<string, double> varDict);
            public abstract string getValue();
            public abstract void setValue(string value);
        }
        class ValNode: Node
        {
            public string val;
            public ValNode(string val)
            {
                this.val = val;
            }
            public override double Eval(Dictionary<string, double> varDict)
            {
                double value = Convert.ToDouble(this.val);
                return value;
            }
            public override string getValue()
            {
                return this.val;
            }
            public override void setValue(string value)
            {
                this.val = value;
            }
        }
        class VarNode: Node
        {
            public string val;
            public VarNode(string val)
            {
                this.val = val;
            }
            public override double Eval(Dictionary<string, double> varDict)
            {
                if (varDict.ContainsKey(this.val))
                {
                    return varDict[this.val];
                }
                else
                    return 0;
               
            }
            public override string getValue()
            {
                return this.val;
            }
            public override void setValue(string value)
            {
                this.val = value;
            }
        }
        public class OpNode : Node
        {
            public string val;
            public Node left, right;
            public OpNode(string value)
            {
                this.val = value;
                this.left = null;
                this.right = null;
            }
            public override double Eval(Dictionary<string, double> varDict)
            {
                switch (this.val)
                {
                    case "+":
                        return (this.left.Eval(varDict) + this.right.Eval(varDict));
                    case "-":
                        return (this.left.Eval(varDict) - this.right.Eval(varDict));
                    case "*":
                        return (this.left.Eval(varDict) * this.right.Eval(varDict));
                    case "/":
                        return (this.left.Eval(varDict) / this.right.Eval(varDict));
                }
                return 0.0;
            }
            public override string getValue()
            {
                return this.val;
            }
            public override void setValue(string value)
            {
                this.val = value;
            }
        }
        public ExpTree(string expression)
        {
            Stack<string> equationStack = new Stack<string>();
            Stack<string> treeStack = new Stack<string>();
            Stack<Node> buildTree = new Stack<Node>();
            string tempString = "";

            //format expression to work with parser
            expression = "(" + expression + ")";

            char[] tokens = expression.ToCharArray();

            //build stack from parsed expression - shunting yard algorithm
            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i] == '(')
                {
                    equationStack.Push(tokens[i].ToString());
                }
                else if (tokens[i] == ')')                      // doesn't push ")" to equation stack
                {
                    while (equationStack.Peek() != "(")         // pop until lhs bracket is found
                    {
                        treeStack.Push(equationStack.Pop());    // add right child and operator
                    }
                    equationStack.Pop();                        // get rid of "("
                }
                else if (operators.Contains(tokens[i].ToString()))         // current token is an operator
                {
                    while(equationStack.Peek() != "(" &&    
                        (!operators.Contains(equationStack.Peek())
                           || (getPrecedence(tokens[i].ToString()) < getPrecedence(equationStack.Peek()))   /*check if stack top is operator of higher precedence*/
                           || (getPrecedence(tokens[i].ToString()) == getPrecedence(equationStack.Peek()))))
                    {
                        treeStack.Push(equationStack.Pop());
                    }
                    equationStack.Push(tokens[i].ToString());
                }
                else     // integer or variable
                {
                    // get numbers with multiple digits (will always execute at least once)
                    while (tokens[i] != '(' && tokens[i] != ')' && !operators.Contains(tokens[i].ToString()))   
                    {
                        tempString += tokens[i];
                        i++;
                    }
                    if (tokens[i] == '(' || tokens[i] == ')' || operators.Contains(tokens[i].ToString()))
                    {
                        i--; //move back so token isn't skipped in next iteration
                    }
                    equationStack.Push(tempString);
                    tempString = "";
                }
            }
            equationStack.Clear();
            //reverse stack to get postfix expression
            while (treeStack.Count != 0)
            {
                equationStack.Push(treeStack.Pop());
            }

            //build tree
            while (equationStack.Count != 0)    //equationStack now contains postfix expression
            {
                tempString = equationStack.Pop();
                if (operators.Contains(tempString))         //make opNode
                {
                    OpNode newNode = new OpNode(tempString);
                    if (buildTree.Count >= 2)
                    {
                        newNode.right = buildTree.Pop();
                        newNode.left = buildTree.Pop();
                        buildTree.Push(newNode);
                    }
                }
                else
                {
                    if (tempString.All(Char.IsDigit))       //make valNode
                    {
                        ValNode newNode = new ValNode(tempString);
                        buildTree.Push(newNode);
                    }
                    else                                    //make varNode
                    {
                        char let = Char.ToUpper(tempString[0]);
                        string row = tempString.Substring(1);
                        tempString = let + row;
                        if (!varDict.ContainsKey(tempString))    
                        {                            
                            varDict[tempString] = 0;    //initialize all variables to 0
                        }
                        VarNode newNode = new VarNode(tempString);
                        buildTree.Push(newNode);
                        varList.Add(tempString);    //add to list of cell references
                    }
                }
            }
            this.root = buildTree.Pop(); //final item in stack will be root node pointing to subtrees
        }
        public int getPrecedence(string op)
        {
            switch(op)
            {
                case "*":
                    return 2;
                case "/":
                    return 2;
                case "-":
                    return 1;
                case "+":
                    return 1;
            }
            return 0;
        }
        public List<string> getVarList()
        {
            return varList;
        }       
        public void SetVar(string varName, double varVal)
        {
            if (varDict.ContainsKey(varName))
            {
                varDict[varName] = varVal;
            }
            else
            {
                varDict.Add(varName, varVal);
            }
        }
        public double Eval()
        {
            if (this.root.getValue().All(char.IsDigit))
            {
                return Convert.ToDouble(this.root.getValue());
            }
            else
                return (this.root.Eval(varDict));
        }
    }
    namespace Spreadsheet
    {
        namespace SpreadsheetEngine 
        {
            //only spreadsheet class can access
            public abstract class Cell : INotifyPropertyChanged
            {
                public Cell(int RowIndex, int ColumnIndex) //ctor
                {
                    this.RowIndex = RowIndex;
                    this.ColumnIndex = ColumnIndex;
                }
                public readonly int RowIndex;
                public readonly int ColumnIndex;
                public string _Text;
                public string Text
                {
                    get { return _Text; }
                    set
                    {
                        if (value != _Text)
                        {
                            _Text = value;
                            FlagPropertyChange("Text"); //initiate event
                        }
                    }
                }
                private string _Value;
                public string Value
                {
                    get { return _Value; }
                    set    
                    {
                        if (value != _Value)
                        {
                            _Value = value;
                            FlagPropertyChange("Value"); //initiate event
                        }
                    }
                }
                public event PropertyChangedEventHandler PropertyChanged;
                private void FlagPropertyChange(string str)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(str));  //?.Invoke() does null check
                }               
            }
        }
    }
}