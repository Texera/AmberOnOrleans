namespace TexeraUtilities {

    public class TexeraTuple
    {
        public ulong seq_token {get; set;}
        public int id {get; set;}
        public string region {get; set;}
        /*
        public string country;
        public string item_type;
        public string sales_channel;
        public string order_priority;
        public string order_date;
        public int order_id;
        public string ship_date;
        */
        public int units_sold {get; set;}
        public float unit_price {get; set;}
        public float unit_cost {get; set;}
        /*
        public float total_revenue;
        public float total_cost;
        public float total_profit;
        */
        public TexeraTuple(ulong seq_token,int id,string[] list) 
        {
            this.seq_token = seq_token;
            this.id = id;
            this.region = "";
            if (list == null)
                return;
            region = list[0];
            units_sold = int.Parse(list[8]);
            unit_price = float.Parse(list[9]);
            unit_cost = float.Parse(list[10]);
        }
    }
    
}