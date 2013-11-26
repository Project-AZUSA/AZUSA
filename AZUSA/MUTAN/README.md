MUTAN 是一組為 AZUSA 設計的簡單語法, 用作 AZUSA 的內部控制和進線程的創建。同時也支持腳本檔執行。
MUTAN aims to define and develop a simple to use set of syntax that will act as the control language to be used on AZUSA. 

MUTAN 的語法定義:
Semiformal definition of MUTAN:

expr  :=  *          (表達式, 不能為空, 目前只支持字串和整數, 例如 1+1, (1>2)&(VAR=3), ~(true&true|false),...)
decla :=  [$]ID=expr (宣稱或改變變量的值, 加 $ 號表示是臨時變量, 不會被保存, 例如 VAR=NYAN , X=2 ,etc.)
exec  :=  RID(expr|\lambda)   (調用進程或指令, 例如 EXIT(), IMG(nyan.png) , SAY(name))
basic  :=  decla|exec
multi :=  basic{;basic}
cond  :=  expr?multi
stmt  :=  basic|multi|cond
stmts :=  stmt{;stmt}
loop  :=  @stmts+ 
line  :=  stmts|loop

(區塊定義)
namedblock :=  
.ID{
block
}

condblock  :=  
expr{
block
}

loopblock :=
@{
block
}

block := (line|namedblock|condblock|loopblock)*
