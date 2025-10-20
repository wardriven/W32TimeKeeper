using TimeKeeperApp.Models;

namespace TimeKeeperApp.Services;

public interface ISettingsStore
{
    TimeCheckSettings Load();

    void Save(TimeCheckSettings settings);
}
