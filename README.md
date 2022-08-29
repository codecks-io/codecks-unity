# Codecks Bug & Feedback Reporter for Unity

## Documentation

For the complete picture head to the [Codecks Manual Page](https://manual.codecks.io/user-reports/).


## Set up

Move the `Assets/Codecks_io/Codecks Bugs & Feedback Reporter` folder into the `Assets/Codecks Bugs & Feedback Reporter` folder of your project. Once imported, you can find a scene named `CodecksSampleScene` that contains a default layout and sample setup for you to check out. Unity will also ask to install the `TextMesh Pro` plugin. This plugin is necessary to run the sample scene.

## Getting started

The sample scene already provides all the UI elements and component setup for you to get started right away. You can test the initial setup of the report tool right from the sample scene by entering your created report token (the one that you created in your Codecks User Report settings screen) into the `Default Token` property of the `CodecksCardCreator` component on the `CardCreator` scene object (found under the `Canvas` object). Once you've done that you can hit _Play_. Click the "Give Feedback!" button, fill out the form and press "Send report". If everything works, you should see a card pop up in your Codecks just moments later. In case of issues, an error message should be printed to your screen and console.

## Adapting it to your own needs

After testing the initial setup, we recommend copying or integrating the sample scene into your own UI game scene where you can configure it to show up when pressing a hotkey or by selecting a menu entry according to your own needs. You may also modify the layout to fit your game thematically or use the Codecks default layout as provided. In any case please make sure to not hide the `Powered by Codecks` sprite and display it next to the report form.

Here's an explanation what the two provided MonoBehavior classes do:

- **CodecksCardCreator** handles the basic API communication with Codecks for the purpose of creating cards inside your Codecks project. This class does not handle any UI related tasks and contains only the basic functionality.
- **CodecksCardCreatorForm** is a helper class that manages the UI and forwards the input to the `CodecksCardCreator` class. You may write your own UI handling in case you're not using the default Unity Canvas system or in case you have special requirements for your UI. The class provides a method `GetMetaText` which you can edit to add your own game related meta data. By default the component also creates a screenshot and attaches it to the request sent to the `CodecksCardCreator` class. You may choose to add additional files to the request (e.g. attaching a savegame or world state dump).

## License

The code is licensed under the MIT license. See [`LICENSE.md`](./LICENSE.md).

## Contribute

### Docs

The sources for the docs can be found in the [`docs.md`](./Assets/Codecks_io/Codecks%20Bug%20%26%20Feedback%20Reporter/Documentation/docs.md) file.

To create a PDF you need node v14+ installed on your machine. Run this command from the [`Documentation`](./Assets/Codecks_io/Codecks%20Bug%20%26%20Feedback%20Reporter/Documentation/) folder:

```sh
cat ./docs.md | npx md-to-pdf > ./Codecks\ Unity\ Plugin\ Manual.pdf
```
