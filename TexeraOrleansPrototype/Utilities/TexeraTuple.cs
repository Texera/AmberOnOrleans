namespace TexeraUtilities {

    public class TexeraTuple
    {
        public string[] FieldList=null;
        private TexeraTuple()
        {
            
        }
        public TexeraTuple(string[] list) 
        {
            if (list != null)
            {
                FieldList=list;
            }
        }
    }
    
}