using System;
using System.Collections.Generic;

[Serializable]
public class MatchState
{
    public string playerXUid;
    public string playerOUid;
    public string currentTurn;
    public string winner;
    public string status;
    public Dictionary<string, string> board;
}