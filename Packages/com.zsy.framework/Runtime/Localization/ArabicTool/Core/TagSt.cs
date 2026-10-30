namespace ArabicTool
{
    public class TagSt
    {
        public string start;
        public string end;
        public int pos;
        public int len;
        public int line;

        public TagSt()
        {
            this.start = "";
            this.end = "";
            this.pos = -1;
            this.len = 0;
            this.line = -1;
        }

        public TagSt(string start, string end, int pos, int len, int line)
        {
            this.start = start;
            this.end = end;
            this.pos = pos;
            this.len = len;
            this.line = line;
        }
    }
}