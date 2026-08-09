using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_Novel_writing_System
{
    public interface IAIService
    {
        Task<string> GenerateResponseAsync(string prompt);
    }
}
