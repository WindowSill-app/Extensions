---
name: dotnet-ui
description: Builds WinUI 3 desktop applications with Windows App SDK. Covers XAML patterns, x:Bind, x:Load, MVVM, MSIX and unpackaged deployment, UWP migration, accessibility, and localization.
license: MIT
user-invocable: true
---

# dotnet-ui

## Overview

WinUI 3 desktop development with Windows App SDK. This skill covers XAML patterns, data binding, MVVM architecture, packaging and deployment, accessibility, and localization.

## Routing Table

| Topic | Keywords | Description | Companion File |
|-------|----------|-------------|----------------|
| WinUI | Windows App SDK, XAML, MSIX/unpackaged | Windows App SDK, x:Bind, x:Load, MSIX/unpackaged, UWP migration | references/winui.md |
| Accessibility | AutomationPeer, high contrast, keyboard nav | AutomationProperties, AutomationPeer, high contrast, keyboard navigation, screen readers | references/accessibility.md |
| Localization | .resw, x:Uid, ResourceLoader, pluralization, RTL | .resw resources, x:Uid XAML binding, MRT resource system, pluralization, RTL | references/localization.md |

## Scope

- WinUI 3 / Windows App SDK
- XAML patterns (x:Bind, x:Load, templates, styles)
- MVVM architecture with MVVM Toolkit
- MSIX and unpackaged deployment
- UWP to WinUI 3 migration
- Accessibility (AutomationProperties, AutomationPeer, high contrast, keyboard navigation)
- Localization (.resw, x:Uid, MRT resource system, pluralization, RTL)

## Out of scope

- Blazor, MAUI, Uno Platform, WPF, WinForms -- not applicable to this project
- Server-side auth middleware and API security configuration -- see [skill:dotnet-api]
- Non-UI testing strategy (unit, integration, E2E architecture) -- see [skill:dotnet-testing]
- Backend API patterns and architecture -- see [skill:dotnet-api]
- Console UI (Terminal.Gui, Spectre.Console) -- see [skill:dotnet-tooling]
