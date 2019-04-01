namespace TexeraUtilities {

    public class TexeraTuple
    {
        public int TableID;
        public string[] FieldList=null;
        public TexeraTuple(int tableId,string[] list) 
        {
            TableID=tableId;
            if (list != null)
            {
                FieldList=list;
            }
        }
    }
    
}