namespace TexeraUtilities {

    public struct TexeraTuple
    {
        public string[] FieldList;
       
        public TexeraTuple(string[] list) 
        {
            if (list != null)
            {
                FieldList=list;
            }
            else
            {
                FieldList=null;
            }
        }
    }
    
}