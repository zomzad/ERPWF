namespace ERPWF
{
    class Program
    {
        static void Main(string[] args)
        {
            //Model mdoel = new Model();
            //mdoel.GetSerpSignFormList();

            //WorkflowModel model = new WorkflowModel();
            //model.GetAllNecessarySignData();

            BatchModel model = new BatchModel();
            model.GetAllNecessarySignData();
        }
    }
}
