using Mergistry.Models;

namespace Mergistry.Services
{
    public interface ISaveService
    {
        // ── Meta progression ─────────────────────────────────────────────────
        MetaProgressionModel LoadMeta();
        void                 SaveMeta(MetaProgressionModel meta);

        // ── Mid-run save ─────────────────────────────────────────────────────
        RunSaveData  LoadRun();
        void         SaveRun(RunSaveData data);
        void         DeleteRunSave();

        bool HasRunSave { get; }
    }
}
