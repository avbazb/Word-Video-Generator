
public ApiService()
{
    _httpClient = new HttpClient();
    _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "YOUR_ALIBABA_CLOUD_API_KEY"); // 请替换为您的阿里云API密钥
}

public async Task<string[]> GetWordTranslationsAsync(string word)
{
    var messages = new[]
    {
        new
        {
            role = "user",
            content = $"请给出英文单词 '{word}' 的1个最常用中文释义，直接返回释义，不要包含例句或其他内容。"
        }
    };

    var request = new
    {
        model = "qwen-flash", // 更换模型
        messages = messages,
        temperature = 0.7,
        top_p = 0.9
    };

    var response = await _httpClient.PostAsync(
        "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation", // qwen-flash API URL
        new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")
    );

}

public async Task<string> GenerateImageAsync(string word)
{
    var request = new
    {
        model = "wan2.2-t2i-flash", // 更换模型
        prompt = $"生成一张关于英文单词 '{word}' 含义的清晰插图，图片风格简洁现代，背景简单，主题突出。",
        size = "1024x1024"
    };

    var response = await _httpClient.PostAsync(
        "https://dashscope.aliyuncs.com/api/v1/services/aigc/text2image/image-synthesis", // wan2.2-t2i-flash API URL
        new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")
    );

}
