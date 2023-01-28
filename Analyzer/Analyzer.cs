using System;
using System.Linq;
using System.Collections.Generic;
//    repeat    errrr := rt +56 -lkygfds[1 , 2 , lh+gfd, r6-34] * 56 ; as := er mod 65 ; 
namespace Analyzer
{
    /// <summary>
    /// Синтаксический анализатор оператора цикла языка Модула-2 вида REPEAT UNTIL;
    /// </summary>
    internal class SyntaxAnalyzer
    {
        /// <summary>
        /// Перечисление состояний графов состояний
        /// </summary>
        //Cyc - состояния графа "оператор цикла"
        //ID - состояния графа "идентификатора"
        //Const - состояния графа "константа"
        //RHS - состояния графа "правая часть"
        //LHS - состояния графа "левая часть"
        //Граф_номер - состояние, соответствующее нарисованному графу под определенным номером
        //Граф_СИМВОЛ{номер} - состояние, по которому перешли через указанный символ
        //Err - ошибочное состояние
        enum State 
        {   Cyc_S, Cyc_R1, Cyc_E1, Cyc_P, Cyc_E2, Cyc_A1, T_1, Cyc_1, Cyc_2, Cyc_Assign, Cyc_3, Cyc_4, Cyc_5, Cyc_U, Cyc_N1, Cyc_T2, Cyc_I1, Cyc_L, Cyc_6, Cyc_N2, Cyc_O, 
            Cyc_T3, Cyc_7, Cyc_Bracket, Cyc_Space1, Cyc_I2, Cyc_N3, Cyc_Space2, Cyc_Space3, Cyc_In, Cyc_InLHS, Cyc_8, Cyc_LessSymb, Cyc_MoreSymb, Cyc_O2, 
            Cyc_R2, Cyc_A2, Cyc_N4, Cyc_D, Cyc_Space4, Cyc_F, 
            Err,
            ID_S, ID_1, ID_F, 
            Const_S, Const, Const_F,
            RHS_S, RHS_1, RHS_SP, RHS_M, RHS_O, RHS_D2, RHS_D1, RHS_I, RHS_V, RHS_2, RHS_F,
            LHS_S, LHS_1, LHS_2, LHS_3, LHS_SP, LHS_Comma, LHS_1stR, LHS_1stL, LHS_M, LHS_O, LHS_D2, LHS_D1, LHS_I, LHS_V, LHS_4, LHS_5, LHS_F
        }
        /// <summary>
        /// Перечисление типов переменных
        /// </summary>
        enum VariableType
        {
            Array, Integer
        }

        Dictionary<string, (VariableType, int)> _variables = new Dictionary<string, (VariableType, int)>();
        /// <summary>
        /// Входная анализируемая строка
        /// </summary>
        readonly string _input;
        /// <summary>
        /// Порядковый номер анализуремого в данный момент символа
        /// </summary>
        int _curCharInd = 0;
        int _curDim;
        /// <summary>
        /// Текущее состояние анализтора
        /// </summary>
        State _curState = State.Cyc_S;


        /// <summary>
        /// Список констант для вывода
        /// </summary>
        public List<int> Constants { get; } = new List<int>();
        public List<string> Identifiers { get
            {
                List<string> identifiers = new List<string>();
                foreach (var item in _variables.ToList())
                    identifiers.Add($"{item.Key} - {(item.Value.Item1 == VariableType.Integer ? "переменная":$"массив с {item.Value.Item2} измерениями")}");
                identifiers.Sort();
                return identifiers;
            } 
        }
        /// <summary>
        /// Нашёл ли анализатор ошибки при обработке
        /// </summary>
        public bool HasErrors { get => _curState == State.Err; }

        /// <summary>
        /// Индекс символа, на котором обнаружилась ошибка
        /// </summary>
        public int? ErrorIndex {
            get {
                if (HasErrors)
                    return _curCharInd;
                return null;
            } 
        }
        /// <summary>
        /// Делегат для отправки сообщений об обработке строки
        /// </summary>
        MessageSender MessageSender { get; set; }

        /// <summary>
        /// Конструктор анализатора
        /// </summary>
        /// <param name="s">Входная строка для анализа</param>
        public SyntaxAnalyzer(string s, MessageSender sender)
        {
            _input = s.ToLower();
            MessageSender = sender;
        }
            

        /// <summary>
        /// Метод для анализа идентификаторов, добавляет удовлетворяющие условию индентификаторы в список
        /// </summary>
        /// <exception cref="Exception">Выбрасывается при нахождении ошибки, содержит ожидаемый символ и индекс ошибки</exception>
        //идентификатор на U делать нельзя, иначе недетерминированный переход из 5 в графе цикла
        //после вызова индекс должен корректироваться
        private void AnalyzeIdentifier(out bool isArray, out string id)
        {
            string identifier = String.Empty;
            _curState = State.ID_S;
            for (int i = _curCharInd;i<_input.Length && _curState!=State.ID_F && _curState != State.Err;i++)
            {
                char curChar = _input[i];
                switch(_curState)
                {
                    case State.ID_S:
                        {
                            if ("abcdefghijklmnopqrstuvwxyz".Contains(curChar))
                                _curState = State.ID_1;
                            else
                                RaiseError("ожидался индентификатор");
                            break;
                        }
                    case State.ID_1:
                        {
                            if (!"abcdefghijklmnopqrstuvwxyz0123456789".Contains(curChar))
                                _curState = State.ID_F;
                            break;
                        }
                }
                if (_curState != State.ID_F)
                    identifier += curChar;
            }

            isArray = _curCharInd + identifier.Length < _input.Length && _input[_curCharInd + identifier.Length] == '[';
            id = isArray?identifier:String.Empty;

            if (identifier.Length > 8)
                RaiseError("длина идентификатора должна быть <= 8");
            if (identifier == "repeat" || identifier == "mod" || identifier == "div"
                || identifier == "and" || identifier == "or" || identifier == "until")
                RaiseError("идентификатор не может совпадать с ключевым словом");
            if (_variables.ContainsKey(identifier) && _variables[identifier].Item1 == VariableType.Array && !isArray)
                RaiseError("дублирование имён идентификаторов");
            if (!_variables.ContainsKey(identifier) && !isArray)
                _variables.Add(identifier, (VariableType.Integer, 0));
            _curCharInd += identifier.Length;
        }


        /// <summary>
        /// Метод для анализа целой константы без знака, добавляет удовлетворяющие условию строки в список
        /// </summary>
        //после вызова индекс корректируется
        private void AnalyzeConstant()
        {
            string constant = String.Empty;
            _curState = State.Const_S;
            for (int i = _curCharInd; i<_input.Length && _curState!=State.Const_F && _curState != State.Err;i++)
            {
                char curChar = _input[i];
                switch(_curState)
                {
                    case State.Const_S:
                        {
                            if ("123456789".Contains(curChar))
                                _curState = State.Const;
                            else if (curChar == '0')
                            {
                                _curState = State.Const_F;
                                constant += curChar;
                            }
                            else
                                RaiseError("ожидалась константа");
                            break;
                        }
                    case State.Const:
                        {
                            if (!"0123456789".Contains(curChar))
                                _curState= State.Const_F;
                            break;
                        }
                }
                if (_curState != State.Const_F)
                    constant += curChar;
            }
            int result;
            bool tryParse = int.TryParse(constant, out result);
            if (result > 32768 || !tryParse)
                RaiseError("Выход константы за границы целочисленного типа");
            if (!Constants.Contains(result))
                Constants.Add(result);
            _curCharInd += constant.Length;
        }


        /// <summary>
        /// Метод для анализа оператора "левая часть>
        /// </summary>
        // после вызова индекс корректируется
        private void AnalyzeLHS(bool checkArray)
        {
            bool isArray = false;
            string id = String.Empty;
            int dimensionsCount = 0;
            _curState = State.LHS_S;
            for (int i = _curCharInd; i < _input.Length && _curState != State.Err && _curState != State.LHS_F; i++)
            {
                char c = _input[i];
                switch (_curState)
                {
                    case State.LHS_S:
                        {
                            AnalyzeIdentifier(out isArray, out id);
                            i = --_curCharInd;
                            _curState = State.LHS_1;
                            break;
                        }
                    case State.LHS_1:
                        {
                            if (c == '[')
                                _curState = State.LHS_2;
                            else if (c == ' ' || c == ':' || c == ')' || c == '+' || c == '-' || c == '<' || c == '>'|| c == '*' || c == ';')
                                _curState = State.LHS_F;
                            else
                                RaiseError("ожидалось '[' или пробел");
                            break;
                        }
                    case State.LHS_2:
                        {
                            if ("0123456789".Contains(c))
                                AnalyzeConstant();
                            else
                                AnalyzeIdentifier(out _, out _);
                            dimensionsCount++;
                            i = --_curCharInd;  
                            _curState = State.LHS_3;
                            break;
                        }
                    case State.LHS_3:
                        {
                            if (c == ']')
                            {
                                _curState = State.LHS_F;
                                _curCharInd++;
                            }
                            else if (c == ' ')
                                _curState = State.LHS_SP;
                            else if (c == ',')
                                _curState = State.LHS_Comma;
                            else if (c == '>')
                                _curState = State.LHS_1stR;
                            else if (c == '<')
                                _curState = State.LHS_1stR;
                            else if (c == '+' || c == '-' || c == '*')
                                _curState = State.LHS_4;
                            else
                                RaiseError(" ожидалась операция, ']' или пробел");
                            break;
                        }
                    case State.LHS_SP:
                        {
                            if (c == ']')
                            {
                                _curState = State.LHS_F;
                                _curCharInd++;
                            }
                            else if (c == 'm')
                                _curState = State.LHS_M;
                            else if (c == 'd')
                                _curState = State.LHS_D1;
                            else if (c == ',')
                                _curState = State.LHS_Comma;
                            else if (c == '>')
                                _curState = State.LHS_1stR;
                            else if (c == '<')
                                _curState = State.LHS_1stL;
                            else if (c == '+' || c == '-' || c == '*')
                                _curState = State.LHS_4;
                            else if (c != ' ')
                                RaiseError("ожидалась операция, ']' или пробел");
                            break;
                        }
                    case State.LHS_M:
                        {
                            if (c == 'o')
                                _curState = State.LHS_O;
                            else
                                RaiseError("ожидалось 'o' в слове MOD");
                            break;
                        }
                    case State.LHS_O:
                        {
                            if (c == 'd')
                                _curState = State.LHS_D2;
                            else
                                RaiseError("ожидалось 'D' в слове MOD");
                            break;
                        }
                    case State.LHS_D2:
                        {
                            if (c == ' ')
                                _curState = State.LHS_4;
                            else
                                RaiseError("после MOD обязателен пробел");
                            break;
                        }
                    case State.LHS_D1:
                        {
                            if (c == 'i')
                                _curState = State.LHS_I;
                            else
                                RaiseError("ожидалось 'I' в слове DIV");
                            break;
                        }
                    case State.LHS_I:
                        {
                            if (c == 'v')
                                _curState = State.LHS_V;
                            else
                                RaiseError("ожидалось 'V' в слове DIV");
                            break;
                        }
                    case State.LHS_V:
                        {
                            if (c == ' ')
                                _curState = State.LHS_4;
                            else
                                RaiseError("после DIV обязателен пробел");
                            break;
                        }
                    case State.LHS_Comma:
                        {
                            if (c == ' ')
                                _curState = State.LHS_2;
                            else
                                RaiseError("после запятой обязателен пробел");
                            break;
                        }
                    case State.LHS_1stR:
                        {
                            if (c == '>')
                                _curState = State.LHS_4;
                            else
                                RaiseError("ожидалось >");
                            break;
                        }
                    case State.LHS_1stL:
                        {
                            if (c == '<')
                                _curState = State.LHS_4;
                            else
                                RaiseError("ожидалось <");
                            break;
                        }
                    case State.LHS_4:
                        {
                            if ("0123456789".Contains(c))
                            {
                                AnalyzeConstant();
                                i = --_curCharInd;
                                _curState = State.LHS_5;
                            }
                            else if (c != ' ') 
                            {
                                AnalyzeIdentifier(out _, out _);
                                i = --_curCharInd;
                                _curState = State.LHS_5;
                            }
                            break;
                        }
                    case State.LHS_5:
                        {
                            
                            if (c == ',')
                                _curState = State.LHS_Comma;
                            else if (c == ']')
                            {
                                _curCharInd++;
                                _curState = State.LHS_F;
                            }
                            else if (c == ' ')
                                _curState = State.LHS_SP;
                            else if (c == '>')
                                _curState = State.LHS_1stR;
                            else if (c == '<')
                                _curState = State.LHS_1stR;
                            else if (c == '+' || c == '-' || c == '*')
                                _curState = State.LHS_4;
                            else
                                RaiseError("ожидалось ']', операция или ','");
                            break;
                        }
                }
                if (_curState != State.LHS_F)
                    _curCharInd++;
            }
            if (isArray && !_variables.ContainsKey(id))
                _variables.Add(id, (VariableType.Array, dimensionsCount));
            else if (isArray && _variables[id].Item2 == 0)
                RaiseError("дублирование имён идентификатора");
            else if (isArray && checkArray && _variables[id].Item2 - dimensionsCount != _curDim)
                RaiseError("несовпадение измерений массива");
            else if (isArray)
                _curDim = _variables[id].Item2 - dimensionsCount;
            
        }
        

        /// <summary>
        /// Метод для анализа оператора "правая часть"
        /// </summary>
        // после вызова индекс корректируется
        private void AnalyzeRHS()
        {
            _curState = State.RHS_S;
            for (int i = _curCharInd; i < _input.Length && _curState != State.Err && _curState != State.RHS_F; i++)
            {
                char c = _input[i];
                switch (_curState)
                {
                    case State.RHS_S:
                        {
                            if ("0123456789".Contains(c))
                                AnalyzeConstant();
                            else
                                AnalyzeLHS(true);
                            i = --_curCharInd;
                            _curState = State.RHS_1;
                            break;
                        }
                    case State.RHS_1:
                        {
                            if (c == ' ')
                                _curState = State.RHS_SP;
                            else if (c == '+' || c == '-' || c == '*')
                                _curState = State.RHS_2;
                            else if (c == ';' || c == ')')
                                _curState = State.RHS_F;
                            else
                                RaiseError("ожидалась операция (MOD, DIV, *, +, -) или отсутствует пробел");
                            break;
                        }
                    case State.RHS_SP:
                        {
                            if (c == 'm')
                                _curState = State.RHS_M;
                            else if (c == 'd')
                                _curState = State.RHS_D1;
                            else if (c == '+' || c == '-' || c == '*')
                                _curState = State.RHS_2;
                            else if (c != ' ')
                                RaiseError("ожидалась операция(MOD, DIV, +, *, -) или пробел");
                            break;
                        }
                    case State.RHS_M:
                        {
                            if (c == 'o')
                                _curState = State.RHS_O;
                            else
                                RaiseError("ожидалось 'O' в слове MOD");
                            break;
                        }
                    case State.RHS_O:
                        {
                            if (c == 'd')
                                _curState = State.RHS_D2;
                            else
                                RaiseError("ожидалось 'D' в слове MOD");
                            break;
                        }
                    case State.RHS_D2:
                        {
                            if (c == ' ')
                                _curState = State.RHS_2;
                            else
                                RaiseError("после MOD обязателен пробел");
                            break;
                        }
                    case State.RHS_D1:
                        {
                            if (c == 'i')
                                _curState = State.RHS_I;
                            else
                                RaiseError("ожидалось 'I' в слове DIV");
                            break;
                        }
                    case State.RHS_I:
                        {
                            if (c == 'v')
                                _curState = State.RHS_V;
                            else
                                RaiseError("ожидалось 'V' в слове DIV");
                            break;
                        }
                    case State.RHS_V:
                        {
                            if (c == ' ')
                                _curState = State.RHS_2;
                            else
                                RaiseError("после DIV обязателен пробел");
                            break;
                        }
                    case State.RHS_2:
                        {
                            if ("0123456789".Contains(c))
                            {
                                AnalyzeConstant();
                                i = --_curCharInd;
                                _curState = State.RHS_1;
                            }
                            else if ("abcdefghijklmnopqrstuvwxyz".Contains(c))
                            {
                                AnalyzeLHS(true);
                                i = --_curCharInd;
                                _curState = State.RHS_1;
                            }
                            else if (c != ' ')
                                _curState = State.RHS_F;
                            break;
                        }
                }
                if (_curState != State.RHS_F)
                    _curCharInd++;
            }

        }


        /// <summary>
        /// Метод, анализирующий всю строку посимвольно и выбрасывающий исключения в случае нахождения ошибок
        /// </summary>
        public void Analyze()
        {
            //флаги для анализа, встретился UNTIL с условием в конце или нет (если строка оборвалась раньше)
            bool untilMet = false, condMet = true;
            for(int i = _curCharInd; i < _input.Length && _curState != State.Err; i++)
            {
               char c = _input[i];
                //Cyc - состояния графа "оператор цикла"
                //ID - состояния графа "идентификатора"
                //Const - состояния графа "константа"
                //RHS - состояния графа "правая часть"
                //LHS - состояния графа "левая часть"
                //Граф_номер - состояние, соответствующее нарисованному графу под определенным номером
                //Граф_СИМВОЛ{номер} - состояние, по которому перешли через указанный символ
                //Err - ошибочное состояние
                switch (_curState)
                {
                    //repeat
                    case State.Cyc_S:
                        {
                            if (c == 'r')
                                _curState = State.Cyc_R1;
                            else if (c != ' ')
                                RaiseError("ожидалось 'R' в слове REPEAT");
                            break;
                        }
                    case State.Cyc_R1:
                        {
                            if (c == 'e') 
                                _curState = State.Cyc_E1;
                            else
                                RaiseError("ожидалось 'E' в слове REPEAT");
                            break;
                        }
                    case State.Cyc_E1:
                        {
                            if (c == 'p') 
                                _curState = State.Cyc_P;
                            else
                                RaiseError("ожидалось 'P' в слове REPEAT");
                            break;
                        }
                    case State.Cyc_P:
                        {
                            if (c == 'e') 
                                _curState = State.Cyc_E2;
                            else
                                RaiseError("ожидалось 'E' в слове REPEAT");
                            break;
                        }
                    case State.Cyc_E2:
                        {
                            if (c == 'a') 
                                _curState = State.Cyc_A1;
                            else
                                RaiseError("ожидалось 'A' в слове REPEAT");
                            break;
                        }
                    case State.Cyc_A1:
                        {
                            if (c == 't') _curState = State.T_1;
                            else
                                RaiseError("ожидалось 'T' в слове REPEAT");
                            break;
                        }
                    case State.T_1:
                        {
                            if (c == ' ') _curState = State.Cyc_1;
                            else
                                RaiseError("после REPEAT обязателен пробел");
                            break;
                        }
                        //операции присваивания
                    case State.Cyc_1:
                        {
                            if (c != ' ' && c != 'u') 
                            {
                                AnalyzeLHS(false);
                                i = --_curCharInd;
                                _curState = State.Cyc_2;
                            }
                            if (c == 'u')
                                _curState = State.Cyc_U;
                            break;
                        }
                    case State.Cyc_2:
                        {
                            if (c == ':') 
                                _curState = State.Cyc_Assign;
                            else if (c != ' ')
                                RaiseError("ожидалось :=");
                            break;
                        }
                    case State.Cyc_Assign:
                        {
                            if (c == '=')
                                _curState = State.Cyc_3;
                            else
                                RaiseError("ожидалось :=");
                            break;
                        }
                    case State.Cyc_3:
                        {
                            if (c != ' ') 
                            {
                                AnalyzeRHS();
                                i = --_curCharInd;
                                _curState = State.Cyc_4;
                            }
                            break;
                        }
                    case State.Cyc_4:
                        {
                            if (c == ';')
                                _curState = State.Cyc_5;
                            else if (c != ' ')
                                RaiseError("ожидалось ;");
                            break;
                        }
                    case State.Cyc_5:
                        {
                            if (c != ' ' && c != 'u')
                            {
                                AnalyzeLHS(false);
                                _curState = State.Cyc_2;
                                i = --_curCharInd;
                            }
                            if (c == 'u')
                                _curState = State.Cyc_U;
                            break;
                        }
                        //until
                    case State.Cyc_U:
                        {
                            //встретили until
                            untilMet = true;
                            condMet = false;
                            if (c == 'n') _curState = State.Cyc_N1;
                            else
                                RaiseError("ожидалось 'N' в UNTIL");
                            break;
                        }
                    case State.Cyc_N1:
                        {
                            if (c == 't') _curState = State.Cyc_T2;
                            else
                                RaiseError("ожидалось 'T' в UNTIL");
                            break;
                        }
                    case State.Cyc_T2:
                        {
                            if (c == 'i')
                                _curState = State.Cyc_I1;
                            else
                                RaiseError("ожидалось 'I' в UNTIL");
                            break;
                        }
                    case State.Cyc_I1:
                        {
                            if (c == 'l')
                                _curState = State.Cyc_L;
                            else
                                RaiseError("ожидалось 'L' в UNTIL");
                            break;
                        }
                    case State.Cyc_L:
                        {
                            condMet = false;
                            if (c == ' ') 
                                _curState = State.Cyc_6;
                            else
                                RaiseError("После UNTIL обязателен пробел");
                            break;
                        }
                    case State.Cyc_6:
                        {
                            //~, not или выражение в скобках
                            if (c == '~') 
                                _curState = State.Cyc_7;
                            else if (c == 'n') 
                                _curState = State.Cyc_N2;
                            else if (c == '(')
                                _curState = State.Cyc_Bracket;
                            else if (c != ' ')
                                RaiseError("ожидался условный оператор");
                            break;
                        }
                    case State.Cyc_7:
                        {
                            if (c == '(')
                                _curState = State.Cyc_Bracket;
                            else if (c != ' ')
                                RaiseError("ожидалось '('");
                            break;
                        }
                    case State.Cyc_N2:
                        {
                            if (c == 'o') 
                                _curState = State.Cyc_O;
                            else 
                                RaiseError("ожидалось 'o' в слове NOT");
                            break;
                        }
                    case State.Cyc_O:
                        {
                            if (c == 't')
                                _curState = State.Cyc_T3;
                            else
                                RaiseError("ожидалось 't' в слове NOT");
                            break;
                        }
                    case State.Cyc_T3:
                        {
                            if (c == ' ')
                                _curState = State.Cyc_7;
                            else
                                RaiseError("ожидался пробел после NOT");
                            break;
                        }
                    case State.Cyc_Bracket:
                        {
                            //встретилось until(
                            condMet = true;
                            if ("0123456789".Contains(c))
                                AnalyzeConstant();
                            else
                                AnalyzeRHS();
                            i = --_curCharInd;
                            _curState = State.Cyc_Space1;
                            break;
                        }
                    case State.Cyc_Space1:
                        {
                            if (c == ')' && i != _input.Length - 1)
                                _curState = State.Cyc_8;
                            else if (c == ')')
                                RaiseError("обязателен символ конца строки ';'");
                            else if (c == ' ')
                                _curState = State.Cyc_I2;
                            else
                                RaiseError("ожидалось ')' или пробел");
                            break;
                        }
                    case State.Cyc_I2:
                        {
                            if (c == 'i')
                                _curState = State.Cyc_N3;
                            else if (c == ')' && i != _input.Length - 1)
                                _curState = State.Cyc_8;
                            else if (c == ')')
                                RaiseError("обязателен символ конца строки ';'");
                            else if (c != ' ')
                                RaiseError("ожидалось 'i' или ')'");
                            break;
                        }
                    case State.Cyc_N3:
                        {
                            if (c == 'n')
                                _curState = State.Cyc_Space2;
                            else
                                RaiseError("ожидалось 'n' в слове IN");
                            break;
                        }
                    case State.Cyc_Space2:
                        {
                            if (c == ' ')
                                _curState = State.Cyc_In;
                            else
                                RaiseError("после IN обязателен пробел");
                            break;
                        }
                    case State.Cyc_In:
                        {
                            AnalyzeLHS(false);
                            _curState = State.Cyc_InLHS;
                            i = --_curCharInd;
                            break;
                        }
                    case State.Cyc_InLHS:
                        {
                            if (c == ')')
                                _curState = State.Cyc_8;
                            else if (c != ' ')
                                RaiseError("ожидалось ')'");
                            break;
                        }
                    case State.Cyc_8:
                        {
                            if (c == ' ')
                                _curState = State.Cyc_Space3;
                            else if (c == ';')
                                _curState = State.Cyc_F;
                            else if (c == '#' || c == '=')
                                _curState = State.Cyc_6;
                            else if (c == '<')
                                _curState = State.Cyc_LessSymb;
                            else if (c == '>')
                                _curState = State.Cyc_MoreSymb;
                            else
                                RaiseError("Ожидался конец оператора или строки");
                            break;
                        }
                    case State.Cyc_Space3:
                        {
                            if (c == 'o')
                                _curState = State.Cyc_O2;
                            else if (c == 'a')
                                _curState = State.Cyc_A2;
                            else if (c == '#' || c == '=' || c == '&')
                                _curState = State.Cyc_6;
                            else if (c == '<')
                                _curState = State.Cyc_LessSymb;
                            else if (c == '>')
                                _curState = State.Cyc_MoreSymb;
                            else if (c == ';')
                                _curState = State.Cyc_F;
                            else if (c != ' ')
                                RaiseError("Ожидался конец оператора или строки");
                            break;
                        }
                    case State.Cyc_O2:
                        {
                            if (c == 'r')
                                _curState = State.Cyc_R2;
                            else
                                RaiseError("Ожидалось 'r' в 'or'");
                            break;
                        }
                    case State.Cyc_R2:
                        {
                            if (c == ' ')
                                _curState = State.Cyc_6;
                            else
                                RaiseError("после OR обязателен пробел");
                            break;
                        }
                    case State.Cyc_A2:
                        {
                            if (c == 'n')
                                _curState = State.Cyc_N4;
                            else
                                RaiseError("ожидалось 'N' в AND");
                            break;
                        }
                    case State.Cyc_N4:
                        {
                            if (c == 'd')
                                _curState = State.Cyc_D;
                            else
                                RaiseError("ожидалось 'D' в AND");
                            break;
                        }
                    case State.Cyc_D:
                        {
                            if (c == ' ')
                                _curState = State.Cyc_6;
                            else
                                RaiseError("после AND обязателен пробел");
                            break;
                        }
                    case State.Cyc_LessSymb:
                        {
                            if (c == '=' || c == ' ' || c == '>')
                                _curState = State.Cyc_6;
                            else
                                RaiseError("ожидалось '=', '>' или пробел");
                            break;
                        }
                    case State.Cyc_MoreSymb:
                        {
                            if (c == '=' || c == ' ')
                                _curState = State.Cyc_6;
                            else
                                RaiseError("ожидалось '=' или пробел");
                            break;
                        }
                        //были ли символы после ;
                    case State.Cyc_F:
                        {
                            MessageSender("Часть строки после ';' не будет обработана");
                            i = _input.Length;
                            break;
                        }
                }
                _curCharInd++;
            }
            //произошёл ли обрыв раньше
            if (!untilMet)
                RaiseError("ожидалось UNTIL или операция");
            if (!condMet)
                RaiseError("после UNTIL обязательно условие");
        }


        /// <summary>
        /// Метод, выбрасывающий исключение с сообщением об ошибке и переводящий состояние в ошибочное
        /// </summary>
        /// <param name="errorMessage">Сообщение об ошибке</param>
        private void RaiseError(string errorMessage)
        {
            _curState = State.Err;
            throw new Exception($"Ошибка: {errorMessage}, символ {_curCharInd+1}");
        }
    }
}
