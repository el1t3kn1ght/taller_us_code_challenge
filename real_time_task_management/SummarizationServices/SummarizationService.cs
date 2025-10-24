using real_time_task_management.Entities;
using System.Text;
using System.Text.Json;

namespace real_time_task_management.SummarizationServices
{
    public class SummarizationService(HttpClient _httpClient, ILogger<SummarizationService> _logger) : ISummarizationService
    {

        public async Task<string> SummarizeTasksAsync(List<TaskItem> taskItems)
        {
            if (!taskItems.Any())
                return "No tasks to summarize.";

            try
            {
                //  I WOULD NEVER LEAVE A SECRET HERE LIKE THIS!
                //  I would read it from user secrets or even Azure Key Vault; But since its just an example I left it there
                string apiKey = "MY SECRET KEY";


                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("No OpenAI API key configured. Using mock summary.");
                }

                var taskDescriptions = string.Join("\n",
                    taskItems.Select(t => $"- {t.Title}: {t.Description}"));

                var prompt = $@"Provide a concise, professional summary of the following tasks. 
                                Include insights about progress, priorities, and any patterns you notice.
                                Keep it under 150 words and use a friendly, encouraging tone.

                            Tasks:
                            {taskDescriptions}";

                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                    new { role = "system", content = "You are a helpful task management assistant that provides insightful summaries." },
                    new { role = "user", content = prompt }
                },
                    max_tokens = 250,
                    temperature = 0.7
                };

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await _httpClient.PostAsync(
                    "https://api.openai.com/v1/chat/completions",
                    new StringContent(
                        JsonSerializer.Serialize(requestBody),
                        Encoding.UTF8,
                        "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"OpenAI API error: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(responseContent);

                var summary = jsonDoc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "Unable to generate summary.";

                _logger.LogInformation("Successfully generated AI summary");
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI API");

                return "Error calling OpenAI API";
            }
        }
    }
}
