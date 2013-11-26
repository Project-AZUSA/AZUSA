using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AZUSA
{
    partial class MUTAN
    {
        // 這裡包含了 MUTAN 語法的所有定義
        // 定義是利用一個判斷方法來實現的
        // 任何字串都可以用判斷方法來判斷是否符合某定義

        //MUTAN 語法的形式定義

        //expr  :=  *          (任何不為空的表達式, 例如 1+1, (1>2)&(VAR=3), ~(true&true|false) 實施時利用表達式解析器判斷是否合法)
        //decla :=  [$]ID=expr (變量宣告, 變量名如以 $ 開頭表示是臨時變量, AZUSA 在退出時不會保存)
        //exec  :=  RID(expr|"")   (函式調用, RID 是函式名, 參數可以是表達式或空白字串)
        //basic  :=  decla|exec
        //multi :=  basic{;basic}
        //cond  :=  expr?multi
        //stmt  :=  basic|multi|cond
        //stmts :=  stmt{;stmt}
        //loop  :=  @stmts+ 
        //line  :=  stmts|loop

        //(區塊的定義)
        //namedblock :=  
        //.ID{
        //block
        //}

        //condblock  :=  
        //expr{
        //block
        //}

        //loopblock :=
        //@{
        //block
        //}

        //block := (line|namedblock|condblock|loopblock)*  (最終定義, 只要是 block 就是 MUTAN)



        //判斷是否一個 decla (宣告)
        static public bool IsDecla(string line)
        {
            //首先得有一個等號
            //first there has to be an equal sign
            if (line.Contains('='))
            {
                //暫存運算結果
                string tmp;

                //以等號為界分割字串
                string[] split = line.Split('=');

                //等號的左手邊應為變數名
                //變數名必須是一個簡單字串, 不能含有運算符或者其他表達式, 我們可以利用表達式運算器算一下, 如果算出來跟原來一樣, 就是合法字串
                //或者左手邊可以是一個已經宣告過的變數名
                //second the left hand side must be a simple string that is not further evaluable ,ie an expression cannot be used as an ID
                // or it can be an existing ID
                if (ExprParser.TryParse(split[0], out tmp) && tmp == split[0].Trim() || Variables.Exist(split[0].Trim()))
                {
                    return true;
                }
            }

            return false;

        }

        //判斷是否一個 exec (函式調用)
        static public bool IsExec(string line)
        {
            //首先要合有一個開括號而且以閉括號結尾
            //first there has to be an open bracket and ends with closed bracket
            if (line.Contains('(') && line.EndsWith(")"))
            {
                //暫存運算結果
                string tmp;

                //以開括號為界分割字串
                string[] split = line.Split('(');

                //開括號的左手邊應為指令名
                //指令名必須是一個簡單字串, 不能含有運算符或者其他表達式, 我們可以利用表達式運算器算一下, 如果算出來跟原來一樣, 就是合法字串
                //second the left hand side must be a simple string that is not further evaluable, ie an expression cannot be used as a RID
                if (ExprParser.TryParse(split[0], out tmp) && tmp == split[0].Trim())
                {
                    return true;

                }
            }

            return false;
        }

        //判斷是否一個 comment (注解)
        static public bool IsComment(string line)
        {
            //以 # 開頭或者是空白行就算注解
            return line.Trim() == "" || line.StartsWith("#");
        }

        //判斷是否一個 basic (基本指令)
        static public bool IsBasic(string line)
        {
            return IsDecla(line) || IsExec(line) || IsComment(line);
        }

        //判斷是否一個 multi (一行多句指令)
        static public bool IsMulti(string line)
        {
            //一行多句指令以 ; 作為分隔標號
            //split each part with ';', each part should be a basic (decla or exec)
            foreach (string part in line.Split(';'))
            {
                //如果有任何一句不是基本指令就不是 multi 
                if (!IsBasic(part))
                {
                    return false;
                }
            }

            return true;
        }

        //判斷是否一個 cond (條件判斷)
        static public bool IsCond(string line)
        {
            //首先得有個問號
            //first there has to be a question mark
            if (line.Contains('?'))
            {
                //以問題分隔開條件和語句
                string[] split = line.Split('?');

                //右手邊的語句必須是一個合法的 multi (留 multi 也包含了所有次級的定義: decla, exec, basic)
                //lastly the right hand side has to be a multi
                if (IsMulti(line.Replace(split[0] + "?", "")))
                {
                    return true;
                }
            }


            return false;
        }

        //判斷是否一個 stmt (陳述)
        static public bool IsStmt(string line)
        {
            //包含 cond 和 multi, 留意 multi 同時包含了 basic 所以也包含了 decla 和 exec
            return IsCond(line) || IsMulti(line);  //what is basic is also a multi
        }

        //判斷是否一個 stmts (一行多句陳述)
        static public bool IsStmts(string line)
        {
            //一行多句陳述以 ; 作為分隔標號
            //split each part with ';', each part should be a stmt
            foreach (string part in line.Split(';'))
            {
                //如果有任何一句不是陳述就不是 multi
                if (!IsStmt(part))
                {
                    return false;
                }
            }

            return true;
        }

        //判斷是否一個 loop (單句循環)
        static public bool IsLoop(string line)
        {
            //首先要以 @ 開首
            //first the line has to start with '@'
            if (line.StartsWith("@"))
            {
                // @ 後面必須是一個 stmts , stmts 也包括了 stmt 和所有次級定義
                //the rest of the line has to be a stmts
                if (IsStmts(line.TrimStart('@')))
                {
                    return true;
                }
            }
            return false;
        }

        //判斷是否一個 line (單行語句)
        static public bool IsLine(string line)
        {
            return IsLoop(line) || IsStmts(line);
        }


        //接下來就是區塊的定義


        //判斷是否一個 namedblock (命名區塊)
        static public bool IsNamedBlock(string[] lines)
        {
            //首先要以 .(名稱){ 的格式開頭, 以 } 結束
            //first the first line has to start with '.', ends with '{'
            //the last line should be "}"
            if (lines[0].Trim().StartsWith(".") && lines[0].Trim().EndsWith("{") && lines[lines.Length - 1].Trim() == "}")
            {
                //取得區塊名
                string ID = lines[0].Trim().Trim('.', '{');

                //暫存運算結果
                string tmp;

                //區塊名必須是一個簡單字串, 不能含有運算符或者其他表達式, 我們可以利用表達式運算器算一下, 如果算出來跟原來一樣, 就是合法字串
                //second the ID should not be further evaluable, and also should not start/end with spaces
                if (ExprParser.TryParse(ID, out tmp) && tmp == ID)
                {
                    //把第一行和最後一行去掉後的內容應該要是一個 block (歸遞定義)
                    //lastly the content should be a block
                    string[] content = new string[lines.Length - 2];
                    for (int i = 1; i < lines.Length - 1; i++)
                    {
                        content[i - 1] = lines[i];
                    }
                    return IsBlock(content);
                }
            }
            return false;
        }

        //判斷是否一個 condblock (條件區塊)
        static public bool IsCondBlock(string[] lines)
        {
            //首先要以 (條件){ 的格式開頭, 以 } 結束
            //first the first line has to ends with '{'
            //the last line should be "}"
            if (lines[0].Trim().EndsWith("{") && lines[lines.Length - 1].Trim() == "}")
            {
                //取得條件
                string cond = lines[0].Trim().Trim('{');

                //暫存運算結果
                string tmp;

                //暫存轉換結果
                bool chk;

                //條件必須是一個合法表達式
                //second the cond should be a valid expression and should be boolean
                if (ExprParser.TryParse(cond, out tmp))
                {
                    //而且運算結果必須可以轉換成布林值
                    if (Boolean.TryParse(tmp, out chk))
                    {
                        //把第一行和最後一行去掉後的內容應該要是一個 block (歸遞定義)
                        //lastly the content should be a block
                        string[] content = new string[lines.Length - 2];
                        for (int i = 1; i < lines.Length - 1; i++)
                        {
                            content[i - 1] = lines[i];
                        }
                        return IsBlock(content);
                    }
                }
            }
            return false;
        }

        //判斷是否一個 loopblock (循環區塊)
        static public bool IsLoopBlock(string[] lines)
        {
            //首先要以 @{ 開頭, 以 } 結束
            //first the first line has to be '@{'
            //the last line should be "}"
            if (lines[0].Trim() == "@{" && lines[lines.Length - 1].Trim() == "}")
            {
                //把第一行和最後一行去掉後的內容應該要是一個 block (歸遞定義)
                //lastly the content should be a block
                string[] content = new string[lines.Length - 2];
                for (int i = 1; i < lines.Length - 1; i++)
                {
                    content[i - 1] = lines[i];
                }
                return IsBlock(content);

            }
            return false;
        }

        //判斷是否一個 block (區塊), 這是 MUTAN 的最終定義
        static public bool IsBlock(string[] lines)
        {
            //括號記數, 開括號 +1, 閉括跑 -1, 最後不等於就表示不平衡
            int bracketcount = 0;

            //狀態變量, 表示當前是否在區塊中
            bool inblock = false;

            //暫存當前區塊的內容
            List<string> content = new List<string>();

            //逐行檢查
            foreach (string line in lines)
            {
                //判斷是否區塊的開首
                //see if it is beginning of a block
                if (line.Trim().EndsWith("{"))
                {
                    //括號 +1
                    bracketcount++;

                    //把 inblock 設成 true, 表示現在位於區塊
                    inblock = true;

                    //把開首這一行加進區塊內容
                    content.Add(line.Trim());
                    continue;
                }

                //判斷是否區塊的結尾
                if (line.Trim() == "}")
                {
                    //括號 -1
                    bracketcount--;

                    //把這一行加進區塊內容
                    content.Add("}");

                    //如果記數是 0 , 表示括號已平衡, 這一行是當前區塊的結尾
                    //如果不是 0 的話表示只是子區塊的結束, 當前區塊還沒有完
                    if (bracketcount == 0)
                    {
                        //設 inblock 為 false, 表示已不在區塊中
                        inblock = false;

                        //然後檢查區塊, 如果所有的定義都不符合就表示不是合法的語法
                        //check the block
                        if (!IsNamedBlock(content.ToArray()) && !IsCondBlock(content.ToArray()) && !IsLoopBlock(content.ToArray()))
                        {
                            return false;
                        }

                        //處理好了以後就清空暫存
                        content.Clear();
                    }
                    continue;
                }

                //如果不是區塊的開始或結束, 而是位於區塊之內的話, 就直接加進內容暫存
                if (inblock)
                {
                    content.Add(line.Trim());
                    continue;
                }

                //如果不在區塊之中就是普通的單行檢查
                if (!IsLine(line.Trim()))
                {
                    return false;
                }
            }

            //如果檢查完畢後發現括號不平衡就表示不合法
            if (bracketcount != 0)
            {
                return false;
            }

            return true;
        }
    }
}
