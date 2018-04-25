namespace ProseTutorial {
    public static class Semantics {
        public static string Substring(string v, int start, int end) => v.Substring(start, end - start);

        public static int? AbsPos(string v, int k)
        {
            //TODO update the return statement to consider the case where k is a negative number
            //if k is positive, you should return k-1 but it k is negative, it represents the one-based index from the right to the left,
            //so you should return the length of v + k + 1. 
            return k - 1;
        }
    }
}