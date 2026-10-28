using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TailSpin.SpaceGame.Web.Models
{
    /// <summary>
    /// Informações sobre a publicação que está no ar: em qual ambiente o site roda,
    /// qual compilação do Azure Pipelines gerou o pacote e qual runtime .NET está ativo.
    /// </summary>
    public class DeploymentInfo
    {
        // "dev", "staging" ou "local".
        public string EnvironmentKey { get; private set; } = "local";
        // Nome amigável do ambiente, em português.
        public string EnvironmentName { get; private set; } = "Computador local";
        // Nome do site no Azure App Service (vazio quando roda localmente).
        public string SiteName { get; private set; } = "";
        // Região do Azure onde o site está hospedado (quando disponível).
        public string Region { get; private set; } = "";
        // Número da compilação gerado pelo Azure Pipelines (ex.: 20261028.3).
        public string BuildNumber { get; private set; } = "";
        // Identificador interno da execução do pipeline.
        public string BuildId { get; private set; } = "";
        // Versão do runtime .NET em execução.
        public string Runtime { get; private set; } = RuntimeInformation.FrameworkDescription;

        // Recado ao vivo, lido de wwwroot/mensagem.txt (vazio = sem faixa).
        public string Message { get; private set; } = "";

        public bool HasBuild => !string.IsNullOrEmpty(BuildNumber);
        public bool HasMessage => !string.IsNullOrEmpty(Message);

        public static DeploymentInfo Load(string webRootPath)
        {
            var info = new DeploymentInfo();

            // O Azure App Service define estas variáveis em todos os sites.
            var siteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "";
            info.SiteName = siteName;
            info.Region = Environment.GetEnvironmentVariable("REGION_NAME") ?? "";

            if (siteName.Contains("staging", StringComparison.OrdinalIgnoreCase))
            {
                info.EnvironmentKey = "staging";
                info.EnvironmentName = "Homologação";
            }
            else if (siteName.Contains("dev", StringComparison.OrdinalIgnoreCase))
            {
                info.EnvironmentKey = "dev";
                info.EnvironmentName = "Desenvolvimento";
            }
            else if (!string.IsNullOrEmpty(siteName))
            {
                info.EnvironmentKey = "cloud";
                info.EnvironmentName = "Azure";
            }

            // O pipeline grava "NomeDoPipeline, BuildId, BuildNumber" em wwwroot/buildinfo.txt.
            try
            {
                var path = Path.Combine(webRootPath ?? "", "buildinfo.txt");
                if (File.Exists(path))
                {
                    var parts = File.ReadAllText(path).Trim().Split(',');
                    if (parts.Length >= 3)
                    {
                        info.BuildNumber = parts[parts.Length - 1].Trim();
                        info.BuildId = parts[parts.Length - 2].Trim();
                    }
                }
            }
            catch (IOException)
            {
                // Sem arquivo de compilação: a página mostra apenas o ambiente.
            }

            // Recado ao vivo: a primeira linha com texto de wwwroot/mensagem.txt.
            try
            {
                var msgPath = Path.Combine(webRootPath ?? "", "mensagem.txt");
                if (File.Exists(msgPath))
                {
                    foreach (var line in File.ReadAllLines(msgPath))
                    {
                        var text = line.Trim();
                        if (text.Length > 0 && !text.StartsWith("#"))
                        {
                            info.Message = text.Length > 160 ? text.Substring(0, 160) : text;
                            break;
                        }
                    }
                }
            }
            catch (IOException)
            {
                // Sem recado: a faixa não aparece.
            }

            return info;
        }
    }
}
