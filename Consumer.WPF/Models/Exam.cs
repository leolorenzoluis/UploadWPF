using System;
using System.Collections.Generic;

namespace Consumer.Models
{
    public class Exam
    {
        public string ExamName { get; set; }
        public string ExamId { get; set; }
        public string Institution { get; set; }
        public DateTime ExamDate { get; set; }

        public List<File> Files { get; set; }

        public Dictionary<string, File> ExamFiles { get; set; }
    }
}