namespace PanoCapture.Process
{
    public class Update
    {
        public Update()
        {
        }

        public Update(int stepNum, int maxSteps, string message)
        {
            StepNumber = stepNum;
            StepTotal = maxSteps;
            StepText = message;
        }

        public int StepNumber { get; set; }

        public int StepTotal { get; set; }

        public string StepText { get; set; }
    }
}