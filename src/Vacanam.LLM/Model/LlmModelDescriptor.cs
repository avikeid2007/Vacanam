namespace Vacanam.LLM.Model;

public sealed record LlmModelDescriptor(
    string FileName,
    string DisplayName,
    string SizeText,
    string VramRequired,
    string Description,
    string DownloadUrl,
    long FileSizeBytes
)
{
    public static IReadOnlyList<LlmModelDescriptor> Catalog { get; } =
    [
        new(
            FileName: "Qwen2.5-0.5B-Instruct-Q4_K_M.gguf",
            DisplayName: "Qwen 2.5 0.5B Instruct",
            SizeText: "~398 MB",
            VramRequired: "< 600 MB RAM",
            Description: "Alibaba Qwen 2.5 0.5B — Excellent grammar accuracy for sub-400MB",
            DownloadUrl: "https://huggingface.co/Qwen/Qwen2.5-0.5B-Instruct-GGUF/resolve/main/qwen2.5-0.5b-instruct-q4_k_m.gguf",
            FileSizeBytes: 398_000_000
        ),
        new(
            FileName: "Llama-3.2-1B-Instruct-Q4_K_M.gguf",
            DisplayName: "Llama 3.2 1B Instruct",
            SizeText: "~808 MB",
            VramRequired: "< 1.2 GB RAM",
            Description: "Meta Llama 3.2 1B — High precision grammar refinement",
            DownloadUrl: "https://huggingface.co/bartowski/Llama-3.2-1B-Instruct-GGUF/resolve/main/Llama-3.2-1B-Instruct-Q4_K_M.gguf",
            FileSizeBytes: 808_000_000
        )
    ];
}
