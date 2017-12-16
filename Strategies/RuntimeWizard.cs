﻿using System.ComponentModel.Design;
using AdvancedWizardControl.Enums;
using AdvancedWizardControl.EventArguments;
using AdvancedWizardControl.Wizard;
using AdvancedWizardControl.WizardPages;

namespace AdvancedWizardControl.Strategies
{
    public class RuntimeWizardStrategy : WizardStrategy
    {
        public RuntimeWizardStrategy(AdvancedWizard wizard)
        {
            _wizard = wizard;
        }

        public override void Loading()
        {
            if (_wizard.HasPages())
            {
                GoToPage(0);
            }
        }

        public override void SetButtonStates()
        {
            if (_wizard.OnLastPage() && _wizard.HasOnePage())
            {
                _wizard.BackButtonEnabled = false;
                _wizard.NextButtonEnabled = false;
                return;
            }

            if (_wizard.OnLastPage() && _wizard.HasExplicitFinishButton())
            {
                _wizard.BackButtonEnabled = true;
                _wizard.NextButtonEnabled = false;
                return;
            }

            if (_wizard.OnLastPage())
            {
                _wizard.BackButtonEnabled = true;
                _wizard.FinishButtonEnabled = true;
                _wizard.SetButtonText("_btnNext", _wizard.FinishButtonText);
                return;
            }

            if (_wizard.OnFirstPage())
            {
                _wizard.BackButtonEnabled = false;
                _wizard.NextButtonEnabled = _wizard.WizardPages.Count > 1;
                _wizard.SetButtonText("_btnNext", _wizard.ReadNextText());
                return;
            }

            if (_wizard.OnAMiddlePage())
            {
                _wizard.BackButtonEnabled = true;
                _wizard.NextButtonEnabled = _wizard.NextButtonEnabledState;
                _wizard.SetButtonText("_btnNext", _wizard.ReadNextText());
            }
        }

        public override void Back(ISelectionService selection)
        {
            if (UserAllowsMoveToProceed(Direction.Backward, out var args))
            {
                MoveToPreviousPage(args);
                SetButtonStates();
            }
        }

        public override void Next(ISelectionService selection)
        {
            if (Finishing()) return;

            if (UserAllowsMoveToProceed(Direction.Forward, out var args) && _wizard.MoreThanOnePageExists())
            {
                MoveToNextPage(args);
                SetButtonStates();
            }
        }

        public override void Cancel() => _wizard.FireCancelEvent();

        public override void Help() => _wizard.FireHelpEvent();

        public override void Finish() => _wizard.FireFinishEvent();

        public override void GoToPage(int pageIndex)
        {
            int index = _wizard.IndexOfCurrentPage();
            _wizard.SelectWizardPage(pageIndex);
            _wizard.StoreIndexOfCurrentPage(index);
            _wizard.CurrentPage.FirePageShowEvent();
            _wizard.FirePageChanged(_wizard.IndexOfCurrentPage());
        }

        public override void GoToPage(AdvancedWizardPage page)
        {
            int index = _wizard.IndexOfCurrentPage();
            _wizard.SelectWizardPage(page);
            _wizard.StoreIndexOfCurrentPage(index);
            SetButtonStates();
            page.FirePageShowEvent();
            _wizard.FirePageChanged(_wizard.IndexOfCurrentPage());
        }

        private bool UserAllowsMoveToProceed(Direction direction, out WizardEventArgs eventArgs)
        {
            eventArgs = direction == Direction.Forward
                ? AttemptMoveToNextPage()
                : AttemptMoveToPreviousPage();

            return eventArgs.AllowPageChange;
        }

        private void MoveToPreviousPage(WizardEventArgs args)
        {
            if (!CanMoveToPreviousPage(args)) return;

            int pageIndex = _wizard.IndexOfCurrentPage();
            _wizard.SelectWizardPage(_wizard.ReadIndexOfPreviousPage());
            _wizard.NextButtonEnabledState = true;
            _wizard.WizardPages[pageIndex].FirePageShowEvent();
            _wizard.FirePageChanged(pageIndex);
        }

        private void MoveToNextPage(WizardEventArgs args)
        {
            if (!CanMoveToNextPage(args)) return;

            _wizard.SelectWizardPage(args.NextPageIndex);
            _wizard.StoreIndexOfCurrentPage(args.CurrentPageIndex);
            _wizard.WizardPages[args.NextPageIndex].FirePageShowEvent();
            _wizard.FirePageChanged(args.NextPageIndex);
            if (NextPageIsLast(args))
            {
                _wizard.FireLastPage();
            }
        }

        private bool NextPageIsLast(WizardEventArgs args) => args.NextPageIndex == _wizard.WizardPages.Count - 1;

        private bool CanMoveToPreviousPage(WizardEventArgs args) => args.NextPageIndex < _wizard.IndexOfCurrentPage();

        private bool CanMoveToNextPage(WizardEventArgs args) => args.NextPageIndex < _wizard.WizardPages.Count;

        private WizardEventArgs AttemptMoveToPreviousPage() => FireBackEvent();

        private WizardEventArgs AttemptMoveToNextPage() => FireNextEvent();

        private WizardEventArgs FireBackEvent() => _wizard.FireBackEvent(_wizard.IndexOfCurrentPage());

        private WizardEventArgs FireNextEvent() => _wizard.FireNextEvent(_wizard.IndexOfCurrentPage());

        private bool Finishing()
        {
            if (!_wizard.OnLastPage() || _wizard.HasExplicitFinishButton()) return false;
            _wizard.FireFinishEvent();
            return true;
        }

        private readonly AdvancedWizard _wizard;
    }
}