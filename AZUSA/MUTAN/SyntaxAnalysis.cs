using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AZUSA
{
    partial class MUTAN{

    //MUTAN 的表達式解析器
    //Used to parse expressions
    static public class ExprParser
    {
        //嘗試解析, 成功回傳 true, 結果輸出到 result
        //失敗回傳 false, 錯誤信息輸出到 result (INVALIDEXPR (無效的表達式) 或 IMBALBRACKET (括號不平衡))
        //Try to evaluate the expression, will output error message if failed
        static public bool TryParse(string _expr, out string result)
        {
            //首先把表達式前後多餘的空白去掉
            //Remove unnecessary spaces
            string expr = _expr.Trim();

            //空白表達式是無效的
            //No empty expression or empty quotation allowed 
            if (expr == "" || expr.Trim('"')=="")
            {
                result = "INVALIDEXPR";
                return false;
            }

            //宣告用來暫存運算值的變量
            //Store intermediate results
            string imd;
            string imd2;
            int tmp;

            //用大 try 包起整個過程, 如果出錯, 就表示表達式有問題, 回傳 INVALIDEXPR 錯誤
            try
            {
                //先檢查表達式是否單純的變數名
                //如果是的話就從變量環境 Variables 讀取其值
                //If it is just a variable, reply with the corresponding value of the variable
                if (Variables.Exist(expr))
                {
                    result = Variables.Read(expr);
                    return true;
                }

                //如果表達式是單純的布林值或整數值, 就直接返回
                //Return directly the value of TRUE, FALSE or integers
                if (expr.Trim().ToUpper() == "TRUE" || expr.Trim().ToUpper() == "FALSE" || Int32.TryParse(expr, out tmp))
                {
                    //這裡是用來去掉整數開頭多餘的零的, 不過如果表達式單純由零組成, 會變成空白, 所以要檢查一下
                    if (expr.Trim().TrimStart('0') != "") //if it is not purely consists of zeros then we can trim off zeros
                    { 
                        result = expr.Trim().TrimStart('0');
                    }
                    else // otherwise just return 0
                    {
                        result = "0";
                    }
                    return true;
                }

                //然後如果表達式沒那麼單純的話, 就開始作進一步處理

                //首先要把引號內的內容保護好, 因為它們是單純的字串, 不應該進行運算
                //如果不保護的話, 像 "3+1" 就會被運算成 "4" 了
                #region Quotation accounting
                //First go through quotation marks to check which operators are in fact part
                //of a string and should not be split/parsed

                //這是用來記錄哪些字符是在引號保護之下的
                List<int> InvalidOp = new List<int>();  //stores the index of operators that should be ignored
                
                //檢查表達式是否含有引號, 沒有的話就跳過這部分
                if (expr.Contains("\""))
                {
                    //這是一個狀態變量, 表示當前的字符是否在引號之內
                    bool inStr = false;

                    //對表達式的每一個字符進行檢查
                    for (int i = 0; i < expr.Length; i++)
                    {
                        //如果是引號, 就變更狀態變量 inStr
                        //如果現在是在引號內 (inStr=true), 遇到引號就表示引號的範圍已結束,所以就改成 false
                        //反之亦然, 但是這樣的話引號就不能出現在字串之中了
                        if (expr[i] == '"')
                        {
                            inStr = !inStr;
                        }
                        else if (inStr)
                        {
                            InvalidOp.Add(i);
                        }
                    }
                }

                #endregion

                //然後是括號, 先從最淺和最後出現的括號處理起, 歸遞利用 TryParse, 把最深的括號優先進行運算 (歸遞下降 Recursive Descent)
                #region Parenthesis accounting                

                //先檢查是否有括號, 沒有的話就跳過這一部分
                if (expr.Contains('('))
                {
                    //記數用的變量, 開括號+1, 閉括號-1, 如果最後不是零就表示括號不平衡
                    int bracketCount = 0;

                    //用來暫存括號內的內容的變量
                    string content = "";

                    //狀態變量, 記錄現在是否在括號內
                    bool inBrac = false;

                    //一個一個字符地檢查
                    for (int i = 0; i < expr.Length; i++)
                    {
                        //如果是開括號, 而且不在引號保護之中
                        if (expr[i] == '(' && !InvalidOp.Contains(i))
                        {
                            //如果現在括號是平衡的, 清空內容 (比如 (3+1)+(2+1), 在碰第二個開括號時應該清掉 content 裡原有的 3+1)
                            if (bracketCount == 0)
                            {
                                content = "";
                            }
                            //否則表示這個括號也在括號之中, 把它加進內容 (比如 ((3+1)+2), content = (3+1)+2 )
                            else
                            {
                                content += "(";
                            }
                            //無論如何, 因為是開括號, 所以記數加一
                            bracketCount++;   
                        
                            //無論原來是不是在括號內, 開括號以後就必定是括號內了, 所以設狀態為 true
                            inBrac = true;
                        }
                        //如果是閉括號, 而且不在引號保護之中  
                        else if (expr[i] == ')' && !InvalidOp.Contains(i))
                        {
                            //先記數減一
                            bracketCount--;

                            //如果關了這個括號後,記數平衡了, 就表示現在已經是括號外了
                            if (bracketCount == 0)
                            {
                                inBrac = false;
                            }
                            //否則表示這個括號也在括號之中, 把它加進內容
                            else
                            {
                                content += ")";
                            }
                            
                        }
                        //對於其他的東西, 如果是在括號之中, 把它加進內容
                        else if (inBrac)
                        {
                            content += expr[i].ToString();
                        }
                    }

                    //整個表達式處理完畢後, 就檢查括號是否平衡
                    if (bracketCount == 0)  //brackets are balanced
                    {
                        //如果是的話, 就把最後記錄到的括號的內容進行運數, 再用其值替換掉本來的表達式, 然後再進行運算
                        if (TryParse(content, out imd))
                        {
                            return TryParse(expr.Replace("(" + content + ")", imd), out result);
                        }
                    }
                    else
                    {
                        result = "IMBALBRACKET";
                        return false;
                    }
                }
                #endregion               

                //如果所有括號都處理掉了,就進行邏輯運算
                #region Logical operators spliting

                if (expr.Contains("&") && !InvalidOp.Contains(expr.IndexOf("&")))
                {
                    if (TryParse(expr.Split('&')[0], out imd) && TryParse(expr.Replace(expr.Split('&')[0] + "&", ""), out imd2))
                    {
                        result = Convert.ToString(Convert.ToBoolean(imd) && Convert.ToBoolean(imd2));
                        return true;
                    }
                }
                if (expr.Contains("|") && !InvalidOp.Contains(expr.IndexOf("|")))
                {
                    if (TryParse(expr.Split('|')[0], out imd) && TryParse(expr.Replace(expr.Split('|')[0] + "|", ""), out imd2))
                    {
                        result = Convert.ToString(Convert.ToBoolean(imd) || Convert.ToBoolean(imd2));
                        return true;
                    }
                }

                if (expr.Contains("=") && !InvalidOp.Contains(expr.IndexOf("=")))
                {
                    //here we insert three branches to check for !=, >= and <=
                    if (expr.Split('=')[0].EndsWith("!"))   // !=
                    {
                        if (TryParse(expr.Split('=')[0].TrimEnd('!'), out imd) && TryParse(expr.Replace(expr.Split('=')[0] + "=", ""), out imd2))
                        {
                            result = Convert.ToString(imd != imd2);
                            return true;
                        }
                    }

                    if (expr.Split('=')[0].EndsWith(">")) // >=
                    {
                        if (TryParse(expr.Split('=')[0].TrimEnd('>'), out imd) && TryParse(expr.Replace(expr.Split('=')[0] + "=", ""), out imd2))
                        {
                            result = Convert.ToString(Convert.ToInt32(imd) >= Convert.ToInt32(imd2));
                            return true;
                        }
                    }

                    if (expr.Split('=')[0].EndsWith("<")) // <=
                    {
                        if (TryParse(expr.Split('=')[0].TrimEnd('<'), out imd) && TryParse(expr.Replace(expr.Split('=')[0] + "=", ""), out imd2))
                        {
                            result = Convert.ToString(Convert.ToInt32(imd) <= Convert.ToInt32(imd2));
                            return true;
                        }
                    }


                    //nothing is preceding '=' so it is just a normal equality check
                    if (TryParse(expr.Split('=')[0], out imd) && TryParse(expr.Replace(expr.Split('=')[0] + "=", ""), out imd2))
                    {
                        result = Convert.ToString(imd == imd2);
                        return true;
                    }
                }

                if (expr.Contains(">") && !InvalidOp.Contains(expr.IndexOf(">")))
                {
                    if (TryParse(expr.Split('>')[0], out imd) && TryParse(expr.Replace(expr.Split('>')[0] + ">", ""), out imd2))
                    {
                        result = Convert.ToString(Convert.ToInt32(imd) > Convert.ToInt32(imd2));
                        return true;
                    }
                }

                if (expr.Contains("<") && !InvalidOp.Contains(expr.IndexOf("<")))
                {
                    if (TryParse(expr.Split('<')[0], out imd) && TryParse(expr.Replace(expr.Split('<')[0] + "<", ""), out imd2))
                    {
                        result = Convert.ToString(Convert.ToInt32(imd) < Convert.ToInt32(imd2));
                        return true;
                    }
                }
                #endregion

                //邏輯運算之後就是加減乘除的運算
                #region String concatenation and arithmetic operators spliting
                if (expr.Contains("+") && !InvalidOp.Contains(expr.IndexOf("+")))
                {
                    //加號因為同時是算術加法和字串合併, 所以要先做檢查, 判斷何者適用
                    bool isStrCat = false;

                    //先分割成左加兩部分
                    string LHS = expr.Split('+')[0];
                    string RHS=expr.Replace(LHS + "+", "").Trim();
                    LHS=LHS.Trim();

                    //每一邊進行檢查, 如是包括在引號之中的, 表示是字串, 應該用字串合並
                    if (LHS.StartsWith("\"") && LHS.EndsWith("\"")) { isStrCat = true; }
                    if (RHS.StartsWith("\"") && RHS.EndsWith("\"")) { isStrCat = true; }

                    if (TryParse(LHS, out imd) && TryParse(RHS, out imd2))
                    {
                        if (isStrCat)
                        {
                            result = imd + imd2;
                            return true;
                        }

                        //如果沒有明示是字串的話, 嘗試把兩邊都當成整數, 如果不行就表示是簡單的字串合併
                        try
                        {
                            result = Convert.ToString(Convert.ToInt32(imd) + Convert.ToInt32(imd2));
                        }
                        catch
                        {
                            result = imd + imd2;
                        }
                        return true;
                    }
                }

                if (expr.Contains("-") && !InvalidOp.Contains(expr.IndexOf("-")))
                {
                    if (TryParse(expr.Split('-')[0], out imd) && TryParse(expr.Replace(expr.Split('-')[0] + "-", ""), out imd2))
                    {
                        result = Convert.ToString(Convert.ToInt32(imd) - Convert.ToInt32(imd2));
                        return true;
                    }
                }

                if (expr.Contains("*") && !InvalidOp.Contains(expr.IndexOf("*")))
                {
                    if (TryParse(expr.Split('*')[0], out imd) && TryParse(expr.Replace(expr.Split('*')[0] + "*", ""), out imd2))
                    {
                        result = Convert.ToString(Convert.ToInt32(imd) * Convert.ToInt32(imd2));
                        return true;
                    }
                }

                if (expr.Contains("/") && !InvalidOp.Contains(expr.IndexOf("/")))
                {
                    if (TryParse(expr.Split('/')[0], out imd) && TryParse(expr.Replace(expr.Split('/')[0] + "/", ""), out imd2))
                    {
                        result = Convert.ToString(Math.Round((double)Convert.ToInt32(imd) / Convert.ToInt32(imd2), 0));
                        return true;
                    }
                }


                #endregion

                //最後就是檢查邏輯"非"運算
                //Negation spliting
                if (expr.StartsWith("~"))
                {
                    if (TryParse(expr.Trim('~'), out imd))
                    {
                        result = Convert.ToString(!Convert.ToBoolean(imd));
                        return true;
                    }
                }

                //如果甚麼都不是, 就單純的認為是一個字串
                //如果前後有引號包好的話, 就去掉引號
                //前後有引號包好不一定就是單純的字串,例如 "ABC"+"DEF"
                //所以放到最後才來檢查, 確保沒有漏掉任何運算
                //If all things fail, treat as a simple string 
                //For properly quoted string, quotation marks are removed
                if (expr.StartsWith("\"") && expr.EndsWith("\""))
                {
                    result = expr.Trim('"');
                    return true;
                }
                else
                {
                    result = expr;
                    return true;
                }

            }
            catch
            {
                //如果失敗的話就返回無效的表達式錯誤
                result = "INVALIDEXPR";
                return false;

            }

        }
    }
    
    //MUTAN 單行指令的解析器
    //Used to determine type of syntax of a single line
    static public class LineParser
    {
        //嘗試解析, 成功回傳 true, 並作成執行物件輸出到 obj
        //失敗回傳 false, obj 返回 null
        static public bool TryParse(string ln, out IRunnable obj)
        {
            //先把多餘的空白去掉
            string line = ln.Trim();

            //如果是空行就回傳一個空物件
            if (line.Trim() == "")
            {
                obj = new empty();
                return true;
            }

            //從上層結構開始檢查, 這樣就保証層級關係的正確性
            //Parsing should begin from large scale structure to small scale structure
            //in order to ensure correct priority
            
            //單行指令只有兩種 loop 或 stmts
            if (IsLoop(line))
            {
                obj = new loop(line);
                return true;
            }

            if (IsStmts(line))
            {
                obj = new stmts(line);
                return true;
            }


            obj = null;
            return false;

        }

    }

    //MUTAN 多行解析器
    //Used to parse a program
    static public class Parser
    {
        //嘗試解析, 成功回傳 true, 並作成執行物件輸出到 obj
        //失敗回傳 false, obj 返回 null
        static public bool TryParse(string[] program, out IRunnable obj,string part="")
        {
            //先把 program 複製下來
            string[] lines=program;

            //如果有指定特定區塊名的話, 先提取指定的部分
            if (part != "")
            {
                //暫存區塊的內容
                List<string> tmp = new List<string>();

                //狀態變量, 表示現在是否在指定區塊中
                bool inpart = false;

                //括號記數, 開括號 +1 , 閉括號 -1
                int bracCount = 0;

                //逐行解析
                foreach (string ln in program)
                {
                    //先對 inpart 為 true 的情況進行處理

                    //如果現在是在指定區塊之中, 而且是區塊開首
                    if (inpart && ln.EndsWith("{"))
                    {
                        //記數 +1
                        bracCount++;

                        //加進區塊內容
                        tmp.Add(ln);

                        continue;
                    }

                    //如果現在是在指定區塊之中, 而且是區塊結束
                    if (inpart && ln.Trim() == "}")
                    {
                        //記數 +1
                        bracCount--;

                        //如果記數為零, 表示這是指區塊的結束, 跳出循環
                        if (bracCount == 0)
                        {
                            break;
                        }
                        //否則就加進內容
                        else
                        {
                            tmp.Add(ln);
                        }
                    }

                    //如果現在是在指定區塊之中, 但不是區塊開首或結束
                    if (inpart)
                    {
                        //加進內容
                        tmp.Add(ln);

                        continue;
                    }

                    //如果是指定區塊的開頭
                    if (ln.Trim().TrimStart('.').TrimEnd('{').Trim() == part)
                    {
                        //記數 +1
                        bracCount++;

                        //inpart 設成 true, 表示現在處於指定區塊內
                        inpart = true;

                        continue;
                    }
                    
                    
                }

                //重新指定 lines, 改成區塊內容
                lines = tmp.ToArray();
            }

            
            //如果 lines 是合法的區塊就創建並返回 true 和相應的物件
            if (IsBlock(lines))
            {
                obj=new block(lines);
                return true;
            }

            //否則返回 false 和 null
            obj = null;
            return false;
        }

    }

    //輔助用的函數
    //用於解析 block (假定了輸入已經過檢查是合法的 MUTAN block)
    //Used to parse a verified block
    static IRunnable[] ParseBlock(string[] lines)
    {
        //這是用來儲存解析出來的執行物件的
        List<IRunnable> objects = new List<IRunnable>();

        //這是用來記錄括號( { } )數的, 開括號 +1 , 閉括號 -1
        //用來提取區塊內容
        int bracketcount = 0;

        //狀態變量, 表示現在是否正處於區塊之中
        bool inblock = false;

        //暫存區塊的內容
        List<string> content = new List<string>();

        //逐行閱讀
        foreach (string line in lines)
        {
            //如果是帶開括號, 表示是區塊的開首
            //see if it is beginning of a block
            if (line.Trim().EndsWith("{"))
            {
                // 記數 +1
                bracketcount++;

                // inblock 設成 true, 表示現在正處於區塊之中
                inblock = true;

                //把這一行加進區塊內容
                content.Add(line.Trim());
                continue;
            }

            //如果是閉括號, 表示是區塊的結尾
            if (line.Trim() == "}")
            {
                // 記數 -1
                bracketcount--;

                // 把這一行加進區塊內容
                content.Add("}");

                // 如果記數歸零了, 表示當前區塊已結束, 開始解析區塊
                // 否則的話, 表示只是子區塊結束, 整個區塊還有其他內容
                if (bracketcount == 0)
                {
                    //區塊已結束, 設 inblock 為 false
                    inblock = false;

                    //解析區塊內容, 把所得結果加進執行物件裡
                    //check the block
                    if (IsLoopBlock(content.ToArray()))
                    {
                        objects.Add(new loopblock(content.ToArray()));
                    }
                    else if (IsCondBlock(content.ToArray()))
                    {
                        objects.Add(new condblock(content.ToArray()));
                    }
                    else if (IsNamedBlock(content.ToArray()))
                    {
                        //命名區塊在執行時是跳過的
                        //只有調用腳本檔直接指明時, 才會取得其內容並執行
                    }

                    //清空暫存內容
                    content.Clear();
                }
                continue;
            }

            //如果現在正處於區塊裡, 而又沒有括號, 就直接把這行加進區塊內容的暫存
            if (inblock)
            {
                content.Add(line.Trim());
                continue;
            }

            //否則的話, 就是區塊外的單行
            if (IsLine(line.Trim()))
            {
                if (IsLoop(line.Trim()))
                {
                    objects.Add(new loop(line.Trim()));
                }
                else
                {
                    objects.Add(new stmts(line.Trim()));
                }
            }
        }

        //解析完畢
        return objects.ToArray();
    }


    }

}
