namespace ProseTutorial {
    public static class Semantics {
        public static string Substring(string v, int start, int end) => v.Substring(start, end - start);

        public static int? AbsPos(string v, int k) {
            return k - 1;
        }
    }
}