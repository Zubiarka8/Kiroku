#if ANDROID
using Android.Media;
using Kirokuu.ZerbitzuakSaioa;
using Microsoft.Maui.LifecycleEvents;
using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.Core.Platforms.Android;

namespace Kirokuu;

public static class JakinarazpenFirebaseAndroid
{
    public static MauiAppBuilder ErantsiJakinarazpenFirebaseAndroid(this MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddAndroid(android =>
            {
                android.OnCreate((activity, _) =>
                {
                    CrossFirebase.Initialize(activity);

                    CrossFirebaseCloudMessaging.Current.NotificationReceived += (_, _) =>
                    {
                        SaiatuErreproduzituTxostenJakinarazpenSoinua();
                    };

                    CrossFirebaseCloudMessaging.Current.TokenChanged += async (_, args) =>
                    {
                        await JakinarazpenFcmTokenarenGordetzailea
                            .SaiatuGordeAdministratzaileTokenaAsync(args?.Token)
                            .ConfigureAwait(false);
                    };
                });
            });
        });

        return builder;
    }

    private static void SaiatuErreproduzituTxostenJakinarazpenSoinua()
    {
        try
        {
            var testuingurua = global::Android.App.Application.Context;
            var mp = MediaPlayer.Create(testuingurua, Resource.Raw.deep_confident);
            if (mp is null)
                return;

            mp.Completion += (_, _) =>
            {
                try
                {
                    mp.Release();
                }
                catch (Exception ex)
                {
                    Android.Util.Log.Warn("KirokuuJakinarazpen", ex.ToString());
                }
            };

            mp.Start();
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("KirokuuJakinarazpen", ex.ToString());
        }
    }
}
#endif
