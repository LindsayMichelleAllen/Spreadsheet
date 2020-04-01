using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CptS321;

namespace ExpressionTreeTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string expression = "(S2+6)+3+5*2";
            string menuOption = "";
            var createTree = new ExpTree(expression);
            do
            {
                Console.WriteLine("Current Expression: {0}", expression);
                Console.WriteLine("1. Enter new expression");
                Console.WriteLine("2. Set a variable value");
                Console.WriteLine("3. Evaluate Tree");
                Console.WriteLine("4. Quit");
                menuOption = Console.ReadLine();

                switch (menuOption)
                {
                    case "1":   /*dict variable is attribute of tree, so when a new tree is created, the variable dictionary will be empty*/
                        Console.Write("Enter new expression: ");
                        expression = Console.ReadLine();
                        createTree = new ExpTree(expression);
                        break;
                    case "2":   /*when variable it changed, preexisting dictionary is altered*/
                        Console.Write("Enter variable name: ");
                        string varName = Console.ReadLine();
                        Console.Write("Enter variable value: ");
                        string varVal = Console.ReadLine();
                        createTree.SetVar(varName, Convert.ToDouble(varVal));
                        break;
                    case "3":
                        Console.WriteLine(createTree.Eval());
                        break;
                    case "4":
                        break;
                }
            } while (menuOption != "4");
        }
    }
}
