using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using WpfApp1.Models;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private readonly Queue<Drone> RegularQueue = new Queue<Drone>();
        private readonly Queue<Drone> ExpressQueue = new Queue<Drone>();
        private readonly List<Drone> FinishedJobs = new List<Drone>();
        private int nextServiceTag = 100;
        private bool isDisplayingJob = false;

        public MainWindow()
        {
            InitializeComponent();
            numServiceTag.Text = nextServiceTag.ToString();
            numServiceTag.IsEnabled = false;
            txtCost.PreviewTextInput += ValidateCostInput;
            RefreshViews();
        }

        public void btnAddNewItem_Click(object sender, RoutedEventArgs e)
        {
            if (isDisplayingJob)
            {
                statusBarText.Text = "Clear selection before adding a new job.";
                return;
            }

            if (string.IsNullOrWhiteSpace(txtClientName.Text) ||
                string.IsNullOrWhiteSpace(txtCost.Text) ||
                string.IsNullOrWhiteSpace(txtProblem.Text) ||
                string.IsNullOrWhiteSpace(txtDroneModel.Text))
            {
                statusBarText.Text = "Please fill in all required fields.";
                return;
            }

            if (!double.TryParse(txtCost.Text, out double cost) || cost < 0)
            {
                statusBarText.Text = "Cost must be a positive number.";
                return;
            }

            var job = new Drone
            {
                ClientName = txtClientName.Text,
                ServiceTag = nextServiceTag,
                ServicePriority = rbExpress.IsChecked == true ? "Express" : "Regular",
                DroneModel = txtDroneModel.Text,
                ServiceCost = cost,
                ServiceProblem = txtProblem.Text
            };

            if (job.ServicePriority == "Express")
                ExpressQueue.Enqueue(job);
            else
                RegularQueue.Enqueue(job);

            nextServiceTag += 10;
            numServiceTag.Text = nextServiceTag.ToString();
            statusBarText.Text = $"Added new {job.ServicePriority} job: Tag {job.ServiceTag}";
            ClearForm();
            RefreshViews();
        }

        // Display details in left panel, disable editing
        public void lvRegularService_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (lvRegularService.SelectedItem is Drone job)
                ShowJobDetails(job);
        }
        public void lvExpressService_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (lvExpressService.SelectedItem is Drone job)
                ShowJobDetails(job);
        }

        // Deselect if the selected item is clicked again
        public void lvRegularService_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = GetListViewItemAt(lvRegularService, e.GetPosition(lvRegularService));
            if (item != null && lvRegularService.SelectedItem == item.DataContext)
            {
                lvRegularService.SelectedItem = null;
                ResetDisplayMode();
                e.Handled = true;
            }
        }
        public void lvExpressService_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = GetListViewItemAt(lvExpressService, e.GetPosition(lvExpressService));
            if (item != null && lvExpressService.SelectedItem == item.DataContext)
            {
                lvExpressService.SelectedItem = null;
                ResetDisplayMode();
                e.Handled = true;
            }
        }

        // Utility to get ListViewItem at mouse position
        private static System.Windows.Controls.ListViewItem GetListViewItemAt(System.Windows.Controls.ListView listView, Point position)
        {
            var element = listView.InputHitTest(position) as DependencyObject;
            while (element != null && !(element is System.Windows.Controls.ListViewItem))
                element = System.Windows.Media.VisualTreeHelper.GetParent(element);
            return element as System.Windows.Controls.ListViewItem;
        }

        private void ShowJobDetails(Drone job)
        {
            isDisplayingJob = true;
            txtClientName.Text = job.ClientName;
            numServiceTag.Text = job.ServiceTag.ToString();
            txtDroneModel.Text = job.DroneModel;
            txtCost.Text = job.ServiceCost.ToString();
            txtProblem.Text = job.ServiceProblem;
            rbRegular.IsChecked = job.ServicePriority == "Regular";
            rbExpress.IsChecked = job.ServicePriority == "Express";

            txtClientName.IsReadOnly = true;
            txtDroneModel.IsReadOnly = true;
            txtCost.IsReadOnly = true;
            txtProblem.IsReadOnly = true;
            rbRegular.IsEnabled = false;
            rbExpress.IsEnabled = false;
            btnAddNewItem.IsEnabled = false;
        }

        private void ResetDisplayMode()
        {
            isDisplayingJob = false;
            txtClientName.IsReadOnly = false;
            txtDroneModel.IsReadOnly = false;
            txtCost.IsReadOnly = false;
            txtProblem.IsReadOnly = false;
            rbRegular.IsEnabled = true;
            rbExpress.IsEnabled = true;
            btnAddNewItem.IsEnabled = true;
            ClearForm();
            numServiceTag.Text = nextServiceTag.ToString();
        }

        public void btnProcessRegular_Click(object sender, RoutedEventArgs e)
        {
            if (lvRegularService.SelectedItem is Drone selected)
            {
                RemoveAndFinishJob(RegularQueue, selected);
                lvRegularService.SelectedItem = null;
                ResetDisplayMode();
            }
            else
            {
                statusBarText.Text = "Select a Regular job to process.";
            }
        }
        public void btnProcessExpress_Click(object sender, RoutedEventArgs e)
        {
            if (lvExpressService.SelectedItem is Drone selected)
            {
                RemoveAndFinishJob(ExpressQueue, selected);
                lvExpressService.SelectedItem = null;
                ResetDisplayMode();
            }
            else
            {
                statusBarText.Text = "Select an Express job to process.";
            }
        }
        private void RemoveAndFinishJob(Queue<Drone> queue, Drone target)
        {
            var temp = queue.Where(x => x != target).ToList();
            queue.Clear();
            foreach (var item in temp) queue.Enqueue(item);

            FinishedJobs.Add(target);
            statusBarText.Text = $"Processed Tag {target.ServiceTag} to Finished Jobs";
            RefreshViews();
        }
        public void lvFinishedList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lvFinishedList.SelectedItem is Drone selected)
            {
                var result = MessageBox.Show(
                    $"Remove finished job Tag {selected.ServiceTag}?",
                    "Confirm Remove",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    FinishedJobs.Remove(selected);
                    statusBarText.Text = $"Removed finished job Tag {selected.ServiceTag}";
                    RefreshViews();
                }
            }
        }
        private void ClearForm()
        {
            if (isDisplayingJob) return;
            txtClientName.Clear();
            txtDroneModel.Clear();
            txtCost.Clear();
            txtProblem.Clear();
            rbRegular.IsChecked = true;
            numServiceTag.Text = nextServiceTag.ToString();
        }
        private void RefreshViews()
        {
            lvRegularService.ItemsSource = null;
            lvExpressService.ItemsSource = null;
            lvFinishedList.ItemsSource = null;
            lvRegularService.ItemsSource = RegularQueue.ToList();
            lvExpressService.ItemsSource = ExpressQueue.ToList();
            lvFinishedList.ItemsSource = FinishedJobs.ToList();
        }
        private void ValidateCostInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]*(\.[0-9]{0,2})?$");
        }
    }
}
