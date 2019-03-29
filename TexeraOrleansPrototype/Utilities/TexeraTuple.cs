namespace TexeraUtilities {

    public class TexeraTuple
    {
        public int CustomResult;
        public int TableID;
        public string[] FieldList=null;
        public TexeraTuple(int tableId,string[] list,int customResult=0) 
        {
            TableID=tableId;
            CustomResult=customResult;
            if (list != null)
            {
                FieldList=list;
            }
        }
    }
    
}